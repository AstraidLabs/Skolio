using Mapster;
using Skolio.Communication.Application.Contracts;
using Skolio.Communication.Domain.Entities;

namespace Skolio.Communication.Application.Mapping;

public sealed class CommunicationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Announcement, AnnouncementContract>();
        config.NewConfig<Conversation, ConversationContract>();
        config.NewConfig<ConversationMessage, ConversationMessageContract>();
        config.NewConfig<Notification, NotificationContract>();
    }
}
