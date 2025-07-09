using System.Windows;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Views;

namespace TraSuaApp.WpfClient
{
    public partial class App : System.Windows.Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //Clipboard.SetText(PasswordHelper.HashPassword("123456"));

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

            //try
            //{
            //    await SeedHelper.SeedNhomSanPhamAsync();
            //    MessageBox.Show("✅ Seed NhomSanPham thành công!");
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"❌ Lỗi seed dữ liệu:\n{ex.Message}");
            //}

            var login = new LoginForm();
            login.Show();
        }
    }
}