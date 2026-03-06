using Mapster;
using MediatR;
using Skolio.Communication.Application.Abstractions;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Domain.Entities;

namespace Skolio.Communication.Application.Conversations;

public sealed class CreateConversationCommandHandler(ICommunicationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<CreateConversationCommand, ConversationContract>
{
    public async Task<ConversationContract> Handle(CreateConversationCommand request, CancellationToken cancellationToken)
    {
        var conversation = Conversation.Create(Guid.NewGuid(), request.SchoolId, request.Topic, request.ParticipantUserIds);
        await commandStore.AddConversationAsync(conversation, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return conversation.Adapt<ConversationContract>(mapsterConfig);
    }
}
