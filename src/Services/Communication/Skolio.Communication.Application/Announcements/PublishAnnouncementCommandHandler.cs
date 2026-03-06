using Mapster;
using MediatR;
using Skolio.Communication.Application.Abstractions;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Domain.Entities;

namespace Skolio.Communication.Application.Announcements;

public sealed class PublishAnnouncementCommandHandler(ICommunicationCommandStore commandStore, TypeAdapterConfig mapsterConfig)
    : IRequestHandler<PublishAnnouncementCommand, AnnouncementContract>
{
    public async Task<AnnouncementContract> Handle(PublishAnnouncementCommand request, CancellationToken cancellationToken)
    {
        var announcement = Announcement.Create(Guid.NewGuid(), request.SchoolId, request.Title, request.Message, request.PublishAtUtc);
        await commandStore.AddAnnouncementAsync(announcement, cancellationToken);
        await commandStore.SaveChangesAsync(cancellationToken);
        return announcement.Adapt<AnnouncementContract>(mapsterConfig);
    }
}
