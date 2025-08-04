using Microsoft.AspNetCore.SignalR.Client;
namespace TraSuaApp.Api.Hubs
{
    public class SignalRClient : ISignalRClient
    {
        private readonly HubConnection _connection;

        public SignalRClient(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();
        }

        public async Task StartAsync()
        {
            await _connection.StartAsync();
        }

        public void OnEntityChanged(Action<string> onChanged)
        {
            _connection.On<string>("EntityChanged", onChanged);
        }
    }
}
