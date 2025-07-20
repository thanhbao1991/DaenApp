using System.Windows;
using System.Windows.Controls;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Views;

namespace TraSuaApp.WpfClient
{
    public partial class App : System.Windows.Application
    {
        private void TextBox_SelectAll(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.SelectAll();
        }

        private void PasswordBox_SelectAll(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox pb)
                pb.SelectAll();
        }
        protected override async void OnStartup(StartupEventArgs e)
        {
            EventManager.RegisterClassHandler(typeof(TextBox),
                        UIElement.GotKeyboardFocusEvent,
                        new RoutedEventHandler(TextBox_SelectAll));

            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.GotMouseCaptureEvent,
                new RoutedEventHandler(TextBox_SelectAll));

            EventManager.RegisterClassHandler(typeof(PasswordBox),
                UIElement.GotKeyboardFocusEvent,
                new RoutedEventHandler(PasswordBox_SelectAll));

            EventManager.RegisterClassHandler(typeof(PasswordBox),
                UIElement.GotMouseCaptureEvent,
                new RoutedEventHandler(PasswordBox_SelectAll));

            base.OnStartup(e);
            //FileViewerWindow a = new FileViewerWindow();
            //a.Show();
            //return;
            ApiClient.OnTokenExpired += () =>
            {
                Current.Dispatcher.Invoke(() =>
                {
                    var loginWindow = new LoginForm();
                    loginWindow.Show();

                    foreach (Window w in Current.Windows)
                    {
                        if (w is not LoginForm)
                            w.Close();
                    }
                });
            };


            // Clipboard.SetText(PasswordHelper.HashPassword("123456"));

            var login = new LoginForm();
            if (login.ShowDialog() == true)
            {
                // ✅ Đã login xong, có token rồi → mới được gọi
                await AppProviders.InitializeAsync();

            }

        }
    }
}