using System.Windows;
using System.Windows.Controls;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Views;

namespace TraSuaApp.WpfClient
{
    public partial class App : System.Windows.Application
    {
        private static Mutex? _mutex;
        private static bool _isLoggingIn = false;

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
            const string mutexName = "TraSuaApp_WpfClient_OnlyOneInstance";
            _mutex = new Mutex(true, mutexName, out bool isNewInstance);

            if (!isNewInstance)
            {
                Shutdown();
                return;
            }

            // Tự động select all khi focus TextBox / PasswordBox
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
            // Mở form đăng nhập lần đầu
            var login = new LoginForm();
            if (login.ShowDialog() == true)
            {
                // ✅ bật loading ngay trong login
                login.SetLoading(true);
                try
                {
                    await AppProviders.InitializeAsync();
                    RegisterTokenExpiredHandler();
                }
                finally
                {
                    login.SetLoading(false);
                    login.Close();
                }
            }
            else
            {
                Shutdown();
            }
        }
        private static void RegisterTokenExpiredHandler()
        {
            // Tránh đăng ký nhiều lần
            ApiClient.OnTokenExpired -= HandleTokenExpired;
            ApiClient.OnTokenExpired += HandleTokenExpired;
        }
        private static void HandleTokenExpired()
        {
            if (_isLoggingIn) return;
            _isLoggingIn = true;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                     {
                         // 🟟 Đóng tất cả cửa sổ (trừ LoginForm nếu có)
                         foreach (Window w in System.Windows.Application.Current.Windows.OfType<Window>().ToList())
                         {
                             if (w is not LoginForm)
                                 w.Close();
                         }

                         // 🟟 Nếu đã có LoginForm đang mở thì focus nó
                         var existingLogin = System.Windows.Application.Current.Windows.OfType<LoginForm>().FirstOrDefault();
                         if (existingLogin != null)
                         {
                             existingLogin.Activate();
                             _isLoggingIn = false;
                             return;
                         }

                         // 🟟 Hiện form đăng nhập
                         var loginWindow = new LoginForm();
                         var result = loginWindow.ShowDialog();

                         if (result == true)
                         {
                             AppProviders.InitializeAsync().Wait();
                             var main = new Dashboard();
                             main.Show();
                         }

                         _isLoggingIn = false;
                     });
        }
    }
}
