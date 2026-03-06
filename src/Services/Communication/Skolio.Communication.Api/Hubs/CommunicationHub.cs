using Microsoft.AspNetCore.SignalR;

namespace Skolio.Communication.Api.Hubs;

public sealed class CommunicationHub : Hub
{
    public Task SubscribeSchool(Guid schoolId) => Groups.AddToGroupAsync(Context.ConnectionId, schoolId.ToString());
    public Task SubscribeConversation(Guid conversationId) => Groups.AddToGroupAsync(Context.ConnectionId, conversationId.ToString());
}
