using Microsoft.AspNetCore.SignalR;

namespace RoomGoHanoi.Hubs;

public class ChatHub : Hub
{
    public Task JoinRoom(string id) =>
        Groups.AddToGroupAsync(Context.ConnectionId, "listing-" + id);

    public Task Send(string id, string sender, string text) =>
        Clients
            .Group("listing-" + id)
            .SendAsync("Receive", sender, text, DateTime.Now.ToString("HH:mm"));
}
