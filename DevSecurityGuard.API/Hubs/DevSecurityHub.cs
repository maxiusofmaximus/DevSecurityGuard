using Microsoft.AspNetCore.SignalR;

namespace DevSecurityGuard.API.Hubs;

/// <summary>
/// SignalR hub for real-time updates to all connected UIs
/// </summary>
public class DevSecurityHub : Hub
{
    public async Task SendConfigUpdate(object config)
    {
        await Clients.All.SendAsync("ConfigUpdated", config);
    }

    public async Task SendActivityUpdate(object activity)
    {
        await Clients.All.SendAsync("ActivityUpdated", activity);
    }

    public async Task SendStatsUpdate(object stats)
    {
        await Clients.All.SendAsync("StatsUpdated", stats);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        Console.WriteLine($"Client connected: {Context.ConnectionId}");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
    }
}
