using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Skolio.Communication.Api.Hubs;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Application.Conversations;

namespace Skolio.Communication.Api.Controllers;
[ApiController]
[Route("api/communication/conversations")]
public sealed class ConversationsController(IMediator mediator, IHubContext<CommunicationHub> hubContext) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ConversationContract>> Create([FromBody] CreateConversationRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateConversationCommand(request.SchoolId, request.Topic, request.ParticipantUserIds), cancellationToken);
        return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
    }

    [HttpPost("messages")]
    public async Task<ActionResult<ConversationMessageContract>> AddMessage([FromBody] AddConversationMessageRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddConversationMessageCommand(request.ConversationId, request.SenderUserId, request.Message, request.SentAtUtc), cancellationToken);
        await hubContext.Clients.Group(request.ConversationId.ToString()).SendAsync("messageAdded", result, cancellationToken);
        return CreatedAtAction(nameof(AddMessage), new { id = result.Id }, result);
    }

    public sealed record CreateConversationRequest(Guid SchoolId, string Topic, IReadOnlyCollection<Guid> ParticipantUserIds);
    public sealed record AddConversationMessageRequest(Guid ConversationId, Guid SenderUserId, string Message, DateTimeOffset SentAtUtc);
}
