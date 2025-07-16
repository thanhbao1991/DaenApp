using Microsoft.AspNetCore.SignalR;

namespace TraSuaApp.Api.Hubs;

public class SignalRHub : Hub
{
    public Task<string> GetConnectionId()
    {
        return Task.FromResult(Context.ConnectionId);
    }
}