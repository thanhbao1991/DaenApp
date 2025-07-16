namespace TraSuaApp.WpfClient.Providers
{
    public interface ISignalRClient
    {
        Task ConnectAsync();

        // Sửa lại để nhận 4 đối số
        void Subscribe(string eventName, Action<string, string, string, string> handler);
    }
}