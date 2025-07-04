using System.Windows;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Views;

namespace TraSuaApp.WpfClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ApiClient.OnTokenExpired += () =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var loginWindow = new LoginWindow();
                    loginWindow.Show();

                    foreach (Window w in System.Windows.Application.Current.Windows)
                    {
                        if (w is not LoginWindow)
                            w.Close();
                    }
                });
            };

            var login = new ProductListWindow();
            login.Show();
        }
    }
}