using MediatR;
using Microsoft.AspNetCore.Mvc;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Application.Notifications;
using Skolio.Communication.Domain.Enums;

namespace Skolio.Communication.Api.Controllers;
[ApiController]
[Route("api/communication/notifications")]
public sealed class NotificationsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<NotificationContract>> Create([FromBody] CreateNotificationRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateNotificationCommand(request.RecipientUserId, request.Title, request.Body, request.Channel), cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    public sealed record CreateNotificationRequest(Guid RecipientUserId, string Title, string Body, NotificationChannel Channel);
}
