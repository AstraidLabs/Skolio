using Mapster;
using MediatR;
using Skolio.Communication.Application.Abstractions;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Domain.Entities;

namespace Skolio.Communication.Application.Conversations;

public sealed class AddConversationMessageCommandHandler(ICommunicationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<AddConversationMessageCommand, ConversationMessageContract>
{
    public async Task<ConversationMessageContract> Handle(AddConversationMessageCommand request, CancellationToken cancellationToken)
    {
        var message = ConversationMessage.Create(Guid.NewGuid(), request.ConversationId, request.SenderUserId, request.Message, request.SentAtUtc);
        await commandStore.AddConversationMessageAsync(message, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return message.Adapt<ConversationMessageContract>(mapsterConfig);
    }
}
