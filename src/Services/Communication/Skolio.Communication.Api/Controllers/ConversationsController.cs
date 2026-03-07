using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Skolio.Communication.Api.Auth;
using Skolio.Communication.Api.Hubs;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Application.Conversations;
using Skolio.Communication.Infrastructure.Persistence;

namespace Skolio.Communication.Api.Controllers;

[ApiController]
[Authorize(Policy = Skolio.Communication.Api.Auth.SkolioPolicies.ParentStudentTeacherRead)]
[Route("api/communication/conversations")]
public sealed class ConversationsController(IMediator mediator, IHubContext<CommunicationHub> hubContext, CommunicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<ConversationContract>>> List([FromQuery] Guid schoolId, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, schoolId)) return Forbid();

        return Ok(await dbContext.Conversations.Where(x => x.SchoolId == schoolId).Select(x => new ConversationContract(x.Id, x.SchoolId, x.Topic, x.ParticipantUserIds)).ToListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationContract>> Detail(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Conversations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, entity.SchoolId)) return Forbid();

        return Ok(new ConversationContract(entity.Id, entity.SchoolId, entity.Topic, entity.ParticipantUserIds));
    }

    [HttpGet("{conversationId:guid}/messages")]
    public async Task<ActionResult<IReadOnlyCollection<ConversationMessageContract>>> Messages(Guid conversationId, CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations.FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);
        if (conversation is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, conversation.SchoolId)) return Forbid();

        return Ok(await dbContext.ConversationMessages.Where(x => x.ConversationId == conversationId).OrderBy(x => x.SentAtUtc).Select(x => new ConversationMessageContract(x.Id, x.ConversationId, x.SenderUserId, x.Message, x.SentAtUtc)).ToListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ConversationContract>> Create([FromBody] CreateConversationRequest request, CancellationToken cancellationToken)
    {
        if (!SchoolScope.HasSchoolAccess(User, request.SchoolId)) return Forbid();

        var result = await mediator.Send(new CreateConversationCommand(request.SchoolId, request.Topic, request.ParticipantUserIds), cancellationToken);
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    [HttpPost("messages")]
    public async Task<ActionResult<ConversationMessageContract>> AddMessage([FromBody] AddConversationMessageRequest request, CancellationToken cancellationToken)
    {
        var conversation = await dbContext.Conversations.FirstOrDefaultAsync(x => x.Id == request.ConversationId, cancellationToken);
        if (conversation is null) return NotFound();
        if (!SchoolScope.HasSchoolAccess(User, conversation.SchoolId)) return Forbid();
        if (!conversation.ParticipantUserIds.Contains(request.SenderUserId)) return Forbid();

        var result = await mediator.Send(new AddConversationMessageCommand(request.ConversationId, request.SenderUserId, request.Message, request.SentAtUtc), cancellationToken);
        await hubContext.Clients.Group(request.ConversationId.ToString()).SendAsync("messageAdded", result, cancellationToken);
        return CreatedAtAction(nameof(AddMessage), new { id = result.Id }, result);
    }

    public sealed record CreateConversationRequest(Guid SchoolId, string Topic, IReadOnlyCollection<Guid> ParticipantUserIds);
    public sealed record AddConversationMessageRequest(Guid ConversationId, Guid SenderUserId, string Message, DateTimeOffset SentAtUtc);
}
