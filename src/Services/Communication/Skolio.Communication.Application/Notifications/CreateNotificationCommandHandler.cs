using Mapster;
using MediatR;
using Skolio.Communication.Application.Abstractions;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Domain.Entities;

namespace Skolio.Communication.Application.Notifications;

public sealed class CreateNotificationCommandHandler(ICommunicationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<CreateNotificationCommand, NotificationContract>
{
    public async Task<NotificationContract> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = Notification.Create(Guid.NewGuid(), request.RecipientUserId, request.Title, request.Body, request.Channel);
        await commandStore.AddNotificationAsync(notification, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return notification.Adapt<NotificationContract>(mapsterConfig);
    }
}
