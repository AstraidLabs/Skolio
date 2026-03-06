using MediatR;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Domain.Enums;

namespace Skolio.Communication.Application.Notifications;

public sealed record CreateNotificationCommand(Guid RecipientUserId, string Title, string Body, NotificationChannel Channel) : IRequest<NotificationContract>;
