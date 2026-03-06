using MediatR;
using Skolio.Communication.Application.Contracts;

namespace Skolio.Communication.Application.Announcements;

public sealed record PublishAnnouncementCommand(Guid SchoolId, string Title, string Message, DateTimeOffset PublishAtUtc) : IRequest<AnnouncementContract>;
