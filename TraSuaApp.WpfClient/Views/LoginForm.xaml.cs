using System.Net.Http.Json;
using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Config;
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

            // 🟟 Khôi phục RememberMe & AutoLogin từ setting
            RememberMeCheckBox.IsChecked = Properties.Settings.Default.Luu;
            AutoLoginCheckBox.IsChecked = Properties.Settings.Default.AutoLogin;

            // Nếu AutoLogin = true nhưng RememberMe = false => ép RememberMe = true
            if (Properties.Settings.Default.AutoLogin && !Properties.Settings.Default.Luu)
            {
                RememberMeCheckBox.IsChecked = true;
            }

            // Điền lại username + password nếu đã lưu
            if (Properties.Settings.Default.Luu)
            {
                UsernameTextBox.Text = Properties.Settings.Default.TaiKhoan;
                string decryptedPassword = SecureHelper.Decrypt(Properties.Settings.Default.MatKhau);
                PasswordBox.Password = decryptedPassword;
            }

            // Disable AutoLogin nếu chưa lưu đăng nhập
            AutoLoginCheckBox.IsEnabled = RememberMeCheckBox.IsChecked == true;

            // 🟟 Nếu đủ điều kiện thì auto login
            if (Properties.Settings.Default.Luu
                && Properties.Settings.Default.AutoLogin
                && !string.IsNullOrWhiteSpace(UsernameTextBox.Text)
                && !string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                Loaded += async (s, e) =>
                {
                    await Task.Delay(1000);
                    //0 NotiHelper.Show("Đang đăng nhập tự động...");
                    LoginButton_Click(null!, null!);
                };
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
                ResetUI();
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

                        // 🟟 Lưu setting chính xác
                        Properties.Settings.Default.Luu = RememberMeCheckBox.IsChecked == true;
                        Properties.Settings.Default.AutoLogin = AutoLoginCheckBox.IsChecked == true;

                        if (Properties.Settings.Default.Luu)
                        {
                            Properties.Settings.Default.TaiKhoan = username;
                            Properties.Settings.Default.MatKhau = SecureHelper.Encrypt(password);
                        }
                        else
                        {
                            // Không lưu tài khoản => cũng không cho AutoLogin
                            Properties.Settings.Default.TaiKhoan = "";
                            Properties.Settings.Default.MatKhau = "";
                            Properties.Settings.Default.AutoLogin = false;
                        }

                        Properties.Settings.Default.Save();

                        // 🟟 Debug để chắc chắn
                        System.Diagnostics.Debug.WriteLine(
                            $"[Login Saved] Luu={Properties.Settings.Default.Luu}, AutoLogin={Properties.Settings.Default.AutoLogin}, User={Properties.Settings.Default.TaiKhoan}");

                        Config.apiChatGptKey = result.Data.TenHienThi;

                        var mainWindow = new Dashboard();
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
                ResetUI();
            }
        }
        private void ResetUI()
        {
            LoginButton.IsEnabled = true;
            LoginProgressBar.Visibility = Visibility.Collapsed;
            Mouse.OverrideCursor = null;
        }

        private void RememberMeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            AutoLoginCheckBox.IsEnabled = true;
        }

        private void RememberMeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AutoLoginCheckBox.IsChecked = false;
            AutoLoginCheckBox.IsEnabled = false;
        }
    }
}