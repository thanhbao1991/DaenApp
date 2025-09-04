using Microsoft.AspNetCore.SignalR.Client;

namespace TraSuaApp.WpfClient.Hubs
{
    public class SignalRClient : ISignalRClient
    {
        private readonly HubConnection _connection;
        private Action? _onDisconnected;
        private Action? _onReconnected;

        public SignalRClient(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            // 🟟 xử lý khi disconnect
            _connection.Closed += async (error) =>
            {
                _onDisconnected?.Invoke();
                await Task.CompletedTask;
            };

            // 🟟 xử lý khi reconnect
            _connection.Reconnected += async (connectionId) =>
            {
                _onReconnected?.Invoke();
                await Task.CompletedTask;
            };
        }

        public async Task ConnectAsync()
        {
            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                    await _connection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ SignalR connect error: {ex.Message}");
                throw;
            }
        }

        public void Subscribe(string eventName, Func<string, string, string, string, Task> handler)
        {
            _connection.On<string, string, string, string>(eventName, async (entity, action, id, senderConnId) =>
            {
                await handler(entity, action, id, senderConnId);
            });
        }

        public void OnDisconnected(Action onDisconnected)
        {
            _onDisconnected = onDisconnected;
        }

        public void OnReconnected(Action onReconnected)
        {
            _onReconnected = onReconnected;
        }

        public async Task<string?> GetConnectionId()
        {
            try
            {
                return await _connection.InvokeAsync<string>("GetConnectionId");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ GetConnectionId error: {ex.Message}");
                return null;
            }
        }
    }
}