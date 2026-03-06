using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Skolio.Communication.Api.Hubs;
using Skolio.Communication.Application.Announcements;
using Skolio.Communication.Application.Contracts;

namespace Skolio.Communication.Api.Controllers;
[ApiController]
[Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.TeacherOrSchoolAdministration)]
[Route("api/communication/announcements")]
public sealed class AnnouncementsController(IMediator mediator, IHubContext<CommunicationHub> hubContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<AnnouncementContract>> Publish([FromBody] PublishAnnouncementRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PublishAnnouncementCommand(request.SchoolId, request.Title, request.Message, request.PublishAtUtc), cancellationToken);
        await hubContext.Clients.Group(request.SchoolId.ToString()).SendAsync("announcementPublished", result, cancellationToken);
        return CreatedAtAction(nameof(Publish), new { id = result.Id }, result);
    }
    public sealed record PublishAnnouncementRequest(Guid SchoolId, string Title, string Message, DateTimeOffset PublishAtUtc);
}
