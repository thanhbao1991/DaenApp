using Microsoft.AspNetCore.SignalR.Client;
using TraSuaApp.WpfClient.Providers;

public class SignalRClient : ISignalRClient
{
    private readonly HubConnection _connection;

    public SignalRClient(string url)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect()
            .Build();
    }

    public async Task ConnectAsync()
    {
        if (_connection == null)
            return;

        // 🟟 Kiểm tra trạng thái trước khi gọi StartAsync
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync();
        }
    }

    public void Subscribe(string eventName, Action<string, string, string, string> handler)
    {
        _connection.On<string, string, string, string?>(eventName, handler);
    }

    public async Task<string?> GetConnectionId()
    {
        try
        {
            return await _connection.InvokeAsync<string>("GetConnectionId");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Lỗi khi gọi GetConnectionId: " + ex.Message);
            return null;
        }
    }
}