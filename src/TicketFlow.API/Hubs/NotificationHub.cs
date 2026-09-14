using Microsoft.AspNetCore.SignalR;

namespace TicketFlow.API.Hubs;

public class NotificationHub : Hub
{
    //eaach user goes to his group based on userId
    // so each user gets a notification
    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
    }
}