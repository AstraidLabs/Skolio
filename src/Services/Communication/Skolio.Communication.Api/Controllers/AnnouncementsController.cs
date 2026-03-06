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
[Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministration)]
[Route("api/communication/announcements")]
public sealed class AnnouncementsController(IMediator mediator, IHubContext<CommunicationHub> hubContext, CommunicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<AnnouncementContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
        => Ok(await dbContext.Announcements.Where(x => x.SchoolId == schoolId).OrderByDescending(x => x.PublishAtUtc).Select(x => new AnnouncementContract(x.Id, x.SchoolId, x.Title, x.Message, x.PublishAtUtc)).ToListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AnnouncementContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Announcements.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? NotFound() : Ok(new AnnouncementContract(entity.Id, entity.SchoolId, entity.Title, entity.Message, entity.PublishAtUtc));
    }

    [HttpPost]
    public async Task<ActionResult<AnnouncementContract>> Publish([FromBody] PublishAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PublishAnnouncementCommand(request.SchoolId, request.Title, request.Message, request.PublishAtUtc), cancellationToken);
        await hubContext.Clients.Group(request.SchoolId.ToString()).SendAsync("announcementPublished", result, cancellationToken);
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }
    public sealed record PublishAnnouncementRequest(Guid SchoolId, string Title, string Message, DateTimeOffset PublishAtUtc);
}
