using System.Windows;

namespace TraSuaApp.WpfClient.Helpers
{
    public static class WindowOwnerHelper
    {
        /// <summary>
        /// Tìm owner an toàn: ưu tiên window chứa <paramref name="context"/>,
        /// sau đó window đang Active, rồi MainWindow. Chỉ trả về khi IsLoaded=true.
        /// </summary>
        public static Window? FindOwner(System.Windows.DependencyObject? context)
        {
            try
            {
                Window? w = null;

                if (context != null)
                    w = Window.GetWindow(context);

                var app = Application.Current;

                if ((w == null || !w.IsLoaded) && app != null)
                {
                    var active = app.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
                    if (active != null && active.IsLoaded) w = active;
                }

                if ((w == null || !w.IsLoaded) && app?.MainWindow != null && app.MainWindow.IsLoaded)
                    w = app.MainWindow;

                return (w != null && w.IsLoaded) ? w : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Chỉ set Owner khi hợp lệ để tránh InvalidOperationException.</summary>
        public static void SetOwnerIfPossible(Window child, Window? owner)
        {
            if (owner != null && owner.IsLoaded)
            {
                try { child.Owner = owner; } catch { /* ignore */ }
            }
        }
    }
}