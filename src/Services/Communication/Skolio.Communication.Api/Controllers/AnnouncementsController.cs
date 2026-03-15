using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Skolio.Communication.Api.Hubs;
using Skolio.Communication.Application.Announcements;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Infrastructure.Persistence;

namespace Skolio.Communication.Api.Controllers;

[ApiController]
[Route("api/communication/announcements")]
public sealed class AnnouncementsController(IMediator mediator, IHubContext<CommunicationHub> hubContext, CommunicationDbContext dbContext, ILogger<AnnouncementsController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<IReadOnlyCollection<AnnouncementContract>>> List([FromQuery] Guid schoolId, [FromQuery] bool? isActive, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        var query = dbContext.Announcements.Where(x => x.SchoolId == schoolId);
        if (isActive.HasValue) query = query.Where(x => x.IsActive == isActive.Value);

        return Ok(await query.OrderByDescending(x => x.PublishAtUtc)
            .Select(x => new AnnouncementContract(x.Id, x.SchoolId, x.Title, x.Message, x.PublishAtUtc, x.IsActive))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
    public async Task<ActionResult<AnnouncementContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        return Ok(new AnnouncementContract(entity.Id, entity.SchoolId, entity.Title, entity.Message, entity.PublishAtUtc, entity.IsActive));
    }

    [HttpPost]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<AnnouncementContract>> Publish([FromBody] PublishAnnouncementRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var result = await mediator.Send(new PublishAnnouncementCommand(request.SchoolId, request.Title, request.Message, request.PublishAtUtc), cancellationToken);
        Audit("communication.school-announcement.published", result.Id, new { request.SchoolId, request.Title });
        await hubContext.Clients.Group(request.SchoolId.ToString()).SendAsync("announcementPublished", result, cancellationToken);
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    [HttpPost("platform")]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.PlatformAdministration)]
    public async Task<ActionResult<AnnouncementContract>> PublishPlatformAnnouncement([FromBody] PublishAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PublishAnnouncementCommand(request.SchoolId, request.Title, request.Message, request.PublishAtUtc), cancellationToken);
        Audit("communication.platform-announcement.published", result.Id, new { request.SchoolId, request.Title });
        await hubContext.Clients.Group(request.SchoolId.ToString()).SendAsync("announcementPublished", result, cancellationToken);
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}/override")]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.PlatformAdminOverride)]
    public async Task<ActionResult<AnnouncementContract>> OverrideAnnouncement(Guid id, [FromBody] OverrideAnnouncementRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OverrideReason)) return this.ValidationField("overrideReason", "Override reason is required.");

        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        entity.OverrideForPlatformSupport(request.Title, request.Message, request.PublishAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit("communication.announcement.override", id, new { request.OverrideReason, request.Title });
        return Ok(new AnnouncementContract(entity.Id, entity.SchoolId, entity.Title, entity.Message, entity.PublishAtUtc, entity.IsActive));
    }

    [HttpPut("{id:guid}/deactivation")]
    [Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministrationOnly)]
    public async Task<ActionResult<AnnouncementContract>> SetActivation(Guid id, [FromBody] SetAnnouncementActivationRequest request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        if (request.IsActive) entity.Activate(); else entity.Deactivate();
        await dbContext.SaveChangesAsync(cancellationToken);

        Audit(request.IsActive ? "communication.announcement.activated" : "communication.announcement.deactivated", id, new { request.IsActive });
        return Ok(new AnnouncementContract(entity.Id, entity.SchoolId, entity.Title, entity.Message, entity.PublishAtUtc, entity.IsActive));
    }

    private void Audit(string actionCode, Guid targetId, object payload)
    {
        var actor = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "unknown";
        logger.LogInformation("AUDIT {ActionCode} actor={Actor} target={TargetId} payload={Payload}", actionCode, actor, targetId, payload);
    }

    public sealed record PublishAnnouncementRequest(Guid SchoolId, string Title, string Message, DateTimeOffset PublishAtUtc);
    public sealed record OverrideAnnouncementRequest(string Title, string Message, DateTimeOffset PublishAtUtc, string OverrideReason);
    public sealed record SetAnnouncementActivationRequest(bool IsActive);
}




