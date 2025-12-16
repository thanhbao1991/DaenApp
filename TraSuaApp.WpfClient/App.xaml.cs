using System.Diagnostics;
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

        protected override void OnStartup(StartupEventArgs e)
        {

            const string mutexName = "TraSuaApp_WpfClient_OnlyOneInstance";
            _mutex = new Mutex(true, mutexName, out bool isNewInstance);

            if (!isNewInstance)
            {
                try
                {
                    var current = Process.GetCurrentProcess();
                    var others = Process.GetProcessesByName(current.ProcessName)
                                        .Where(p => p.Id != current.Id);
                    foreach (var p in others)
                    {
                        try
                        {
                            p.Kill();
                            p.WaitForExit(2000);
                        }
                        catch { }
                    }
                }
                catch { }
            }

            // Tự động select-all cho TextBox / PasswordBox
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

            // Đăng ký handler hết hạn token: lần login lại sau sẽ tự init ngay trong LoginForm


            RegisterTokenExpiredHandler();

            // 🟟 Mở Login — LoginForm sẽ tự: login → hiển thị tiến trình load → init AppProviders → start TTS → mở Dashboard
            var login = new LoginForm();
            if (login.ShowDialog() != true)
            {
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }

        private static readonly DependencyProperty _attachedProperty =
            DependencyProperty.RegisterAttached("FadeAttached", typeof(bool), typeof(Window), new PropertyMetadata(false));

        private static void RegisterTokenExpiredHandler()
        {
            ApiClient.OnTokenExpired -= HandleTokenExpired;
            ApiClient.OnTokenExpired += HandleTokenExpired;
        }

        private static void HandleTokenExpired()
        {
            if (_isLoggingIn) return;
            _isLoggingIn = true;

            Application.Current.Dispatcher.Invoke(() =>
            {
                // Đóng tất cả cửa sổ (trừ LoginForm)
                foreach (Window w in Application.Current.Windows.OfType<Window>().ToList())
                {
                    if (w is not LoginForm)
                        w.Close();
                }

                // Nếu LoginForm đã mở → focus lại
                var existingLogin = Application.Current.Windows.OfType<LoginForm>().FirstOrDefault();
                if (existingLogin != null)
                {
                    existingLogin.Activate();
                    _isLoggingIn = false;
                    return;
                }

                // Hiện form đăng nhập (LoginForm sẽ tự xử lý init + TTS + mở Dashboard)
                var loginWindow = new LoginForm();
                loginWindow.ShowDialog();

                _isLoggingIn = false;
            });
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            //AppShippingHelperText.DisposeDriver();
        }
    }
}