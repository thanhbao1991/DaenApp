using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TraSuaApp.WpfClient.Helpers
{
    public class NotifyItem : INotifyPropertyChanged
    {
        public DateTime Time { get; set; } = DateTime.Now;
        public string Message { get; set; } = "";
        public string Kind { get; set; } = "info"; // info | success | warn | error

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnProp([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public static class NotiCenter
    {
        public static ObservableCollection<NotifyItem> Items { get; } = new();
        public static int MaxItems { get; set; } = 80;

        public static void Add(string message, string kind = "info")
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            System.Windows.
                         Application.Current.Dispatcher.Invoke(() =>
                     {
                         Items.Insert(0, new NotifyItem { Message = message.Trim(), Kind = kind, Time = DateTime.Now });
                         while (Items.Count > MaxItems) Items.RemoveAt(Items.Count - 1);
                     });
        }

        public static void Clear() =>
          System.Windows.Application.Current.Dispatcher.Invoke(() => Items.Clear());
    }
}