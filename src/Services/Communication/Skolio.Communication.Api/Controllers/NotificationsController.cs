using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Application.Notifications;
using Skolio.Communication.Domain.Enums;
using Skolio.Communication.Infrastructure.Persistence;

namespace Skolio.Communication.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
[Route("api/communication/notifications")]
public sealed class NotificationsController(IMediator mediator, CommunicationDbContext dbContext) : ControllerBase
{
    private const int MaxPageSize = 200;

    [HttpGet]
    public async Task<ActionResult<PagedResult<NotificationContract>>> List([FromQuery] Guid recipientUserId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
    {
        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || actorUserId != recipientUserId) return Forbid();
        }

        var normalizedPageNumber = Math.Max(pageNumber, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = dbContext.Notifications.Where(x => x.RecipientUserId == recipientUserId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Id)
            .Skip((normalizedPageNumber - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .Select(x => new NotificationContract(x.Id, x.RecipientUserId, x.Title, x.Body, x.Channel))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResult<NotificationContract>(items, normalizedPageNumber, normalizedPageSize, totalCount));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NotificationContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Notifications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();

        if (User.IsInRole("Teacher") && !User.IsInRole("SchoolAdministrator") && !User.IsInRole("PlatformAdministrator"))
        {
            var actorUserId = ResolveActorUserId();
            if (actorUserId == Guid.Empty || actorUserId != entity.RecipientUserId) return Forbid();
        }

        return Ok(new NotificationContract(entity.Id, entity.RecipientUserId, entity.Title, entity.Body, entity.Channel));
    }

    [HttpPost]
    public async Task<ActionResult<NotificationContract>> Create([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateNotificationCommand(request.RecipientUserId, request.Title, request.Body, request.Channel), cancellationToken);
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    private Guid ResolveActorUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(raw, out var actorUserId) ? actorUserId : Guid.Empty;
    }

    public sealed record CreateNotificationRequest(Guid RecipientUserId, string Title, string Body, NotificationChannel Channel);
    public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int PageNumber, int PageSize, int TotalCount);
}
