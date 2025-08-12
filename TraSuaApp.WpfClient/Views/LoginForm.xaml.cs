using System.Net.Http.Json;
using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class LoginForm : Window
    {
        private readonly WpfErrorHandler _errorHandler;

        public LoginForm()
        {
            InitializeComponent();
            _errorHandler = new WpfErrorHandler(ErrorTextBlock);

            if (Properties.Settings.Default.Luu)
            {
                UsernameTextBox.Text = Properties.Settings.Default.TaiKhoan;
                string decryptedPassword = SecureHelper.Decrypt(Properties.Settings.Default.MatKhau);
                PasswordBox.Password = decryptedPassword;
                RememberMeCheckBox.IsChecked = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(null!, null!);
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            _errorHandler.Clear();
            LoginButton.IsEnabled = false;
            LoginProgressBar.Visibility = Visibility.Visible;
            Mouse.OverrideCursor = Cursors.Wait;

            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _errorHandler.Handle(new Exception("Vui lòng nhập đầy đủ tài khoản và mật khẩu."), "Đăng nhập");
                LoginButton.IsEnabled = true;
                LoginProgressBar.Visibility = Visibility.Collapsed;
                Mouse.OverrideCursor = null;
                return;
            }

            var request = new LoginRequest
            {
                TaiKhoan = username,
                MatKhau = password
            };

            try
            {
                var response = await ApiClient.PostAsync("/api/auth/login", request, includeToken: false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Result<LoginResponse>>();

                    if (result != null && result.IsSuccess && result.Data != null)
                    {
                        var login = result.Data;

                        ApiClient.SetToken(login.Token);

                        if (RememberMeCheckBox.IsChecked == true)
                        {
                            Properties.Settings.Default.TaiKhoan = username;
                            Properties.Settings.Default.MatKhau = SecureHelper.Encrypt(password);
                            Properties.Settings.Default.Luu = true;
                        }
                        else
                        {
                            Properties.Settings.Default.TaiKhoan = "";
                            Properties.Settings.Default.MatKhau = "";
                            Properties.Settings.Default.Luu = false;
                        }

                        Properties.Settings.Default.Save();

                        var role = JwtHelper.GetRole(login.Token!);
                        var userId = JwtHelper.GetUserId(login.Token!);

                        var mainWindow = new Dashboard
                        {
                            //    VaiTro = role ?? "NhanVien",
                            //    UserId = userId ?? "",
                            //    TenHienThi = login.TenHienThi ?? "Người dùng"
                            //
                        };

                        mainWindow.Show();
                        this.DialogResult = true;
                        this.Close();
                    }
                    else
                    {
                        _errorHandler.Handle(new Exception(result?.Message ?? "Đăng nhập thất bại."), "Đăng nhập");
                    }
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _errorHandler.Handle(new Exception(content), "Đăng nhập");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "LoginButton_Click");
            }
            finally
            {
                LoginButton.IsEnabled = true;
                LoginProgressBar.Visibility = Visibility.Collapsed;
                Mouse.OverrideCursor = null;
            }
        }

        public void SetLoading(bool isLoading)
        {
            LoginProgressBar.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            LoginButton.IsEnabled = !isLoading;
            Mouse.OverrideCursor = isLoading ? Cursors.Wait : null;
        }
    }
}
