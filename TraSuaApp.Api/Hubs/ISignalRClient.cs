namespace TraSuaApp.Api.Hubs
{
    public interface ISignalRClient
    {
        Task StartAsync();
        void OnEntityChanged(Action<string> onChanged);
    }

}
