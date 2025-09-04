namespace TraSuaApp.WpfClient.Hubs
{
    public interface ISignalRClient
    {
        Task ConnectAsync();
        Task<string?> GetConnectionId();

        void Subscribe(string eventName, Func<string, string, string, string, Task> handler);

        void OnDisconnected(Action onDisconnected);
        void OnReconnected(Action onReconnected);
    }
}