namespace TraSuaApp.WpfClient.Helpers
{
    public sealed class DebounceDispatcher
    {
        private CancellationTokenSource? _cts;
        public void Debounce(int milliseconds, Action action)
        {
            _cts?.Cancel();
            var cts = _cts = new CancellationTokenSource();
            Task.Delay(milliseconds, cts.Token)
                .ContinueWith(t => { if (!t.IsCanceled) action(); },
                              TaskScheduler.FromCurrentSynchronizationContext());
        }
    }

    public sealed class DebounceManager
    {
        private readonly Dictionary<string, DebounceDispatcher> _map = new();
        public void Debounce(string key, int milliseconds, Action action)
        {
            if (!_map.TryGetValue(key, out var d)) _map[key] = d = new DebounceDispatcher();
            d.Debounce(milliseconds, action);
        }
    }
}