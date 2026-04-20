using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.WpfClient.DataProviders;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Hubs;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class LoginForm : Window
    {
        private readonly WpfErrorHandler _errorHandler;

        private TextBlock? _statusText;
        private CancellationTokenSource? _progressCts;

        private static CongViecNoiBoTtsService? _cvTtsSingleton;

        private static async void OnCongViecChanged()
        {
            try
            {
                if (_cvTtsSingleton?.Enabled == true)
                    await _cvTtsSingleton.KickAsync();
            }
            catch
            {
                /* ignore */
            }
        }

        public LoginForm()
        {
            InitializeComponent();

            _errorHandler = new WpfErrorHandler(ErrorTextBlock);
            _statusText = this.FindName("LoadingStatusText") as TextBlock ?? ErrorTextBlock;

            RememberMeCheckBox.IsChecked = Properties.Settings.Default.Luu;
            AutoLoginCheckBox.IsChecked = Properties.Settings.Default.AutoLogin;

            if (Properties.Settings.Default.AutoLogin && !Properties.Settings.Default.Luu)
            {
                RememberMeCheckBox.IsChecked = true;
            }

            if (Properties.Settings.Default.Luu)
            {
                UsernameTextBox.Text = Properties.Settings.Default.TaiKhoan;

                try
                {
                    if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.MatKhau))
                    {
                        string decryptedPassword = SecureHelper.Decrypt(Properties.Settings.Default.MatKhau);
                        PasswordBox.Password = decryptedPassword;
                    }
                }
                catch
                {
                    PasswordBox.Password = "";
                }
            }

            AutoLoginCheckBox.IsEnabled = RememberMeCheckBox.IsChecked == true;

            sv.Text = "Đang tìm server...";

            if (Properties.Settings.Default.Luu
                && Properties.Settings.Default.AutoLogin
                && !string.IsNullOrWhiteSpace(UsernameTextBox.Text)
                && !string.IsNullOrWhiteSpace(PasswordBox.Password))
            {
                Loaded += async (s, e) =>
                {
                    await Task.Delay(1000);
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
            LoginProgressBar.IsIndeterminate = false;
            Mouse.OverrideCursor = Cursors.Wait;

            var username = UsernameTextBox.Text.Trim();
            var password = PasswordBox.Password.Trim();

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
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
                SetLoadingStatus("Đang tìm server...", 5);

                var serverUrl = await ApiClient.DetectBestServerAsync();
                ApiClient.SetBaseUrl(serverUrl);

                sv.Text = $"{serverUrl.TrimEnd('/')}/hub/entity";

                SetLoadingStatus("Đang gửi thông tin đăng nhập...", 10);

                var response = await ApiClient.PostAsync("/api/auth/login", request, includeToken: false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Result<LoginResponse>>();

                    if (result != null && result.IsSuccess && result.Data != null)
                    {
                        var login = result.Data;

                        ApiClient.SetToken(login.Token);

                        Properties.Settings.Default.Luu = RememberMeCheckBox.IsChecked == true;
                        Properties.Settings.Default.AutoLogin = AutoLoginCheckBox.IsChecked == true;

                        if (Properties.Settings.Default.Luu)
                        {
                            Properties.Settings.Default.TaiKhoan = username;
                            Properties.Settings.Default.MatKhau = SecureHelper.Encrypt(password);
                        }
                        else
                        {
                            Properties.Settings.Default.TaiKhoan = "";
                            Properties.Settings.Default.MatKhau = "";
                            Properties.Settings.Default.AutoLogin = false;
                        }

                        Properties.Settings.Default.Save();

                        System.Diagnostics.Debug.WriteLine(
                            $"[Login Saved] Luu={Properties.Settings.Default.Luu}, AutoLogin={Properties.Settings.Default.AutoLogin}, User={Properties.Settings.Default.TaiKhoan}");

                        _progressCts?.Cancel();
                        _progressCts = new CancellationTokenSource();
                        var token = _progressCts.Token;

                        var simulateTask = SimulateProgressAsync(maxPercent: 85, token);

                        SetLoadingStatus("Đang kết nối máy chủ.", 20);

                        SetLoadingStatus("Đang tải dữ liệu hệ thống.", 30);
                        await AppProviders.EnsureCreatedAsync();

                        SetLoadingStatus("Đang tải dữ liệu hoá đơn & danh mục…", 40);
                        await AppProviders.InitializeAsync();

                        AppProviders.SignalR = new SignalRClient(sv.Text);
                        await AppProviders.SignalR.ConnectAsync();

                        if (_cvTtsSingleton == null)
                        {
                            _cvTtsSingleton = new CongViecNoiBoTtsService
                            {
                                Enabled = true,
                                TopN = 5,
                                Interval = TimeSpan.FromMinutes(5)
                            };
                        }

                        if (username == "admin")
                            _cvTtsSingleton.Start();

                        _progressCts.Cancel();
                        await Task.WhenAny(simulateTask, Task.Delay(50));
                        SetLoadingStatus("Hoàn tất. Đang khởi động giao diện...", 100);

                        await Task.Delay(200);

                        var mainWindow = new Dashboard();
                        mainWindow.Show();

                        DialogResult = true;
                        Close();
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

        private void SetLoadingStatus(string text, int? percent = null)
        {
            try
            {
                if (_statusText != null)
                {
                    _statusText.Text = text;
                    _statusText.Foreground = System.Windows.Media.Brushes.White;
                    _statusText.Opacity = 0.95;
                }

                if (percent.HasValue)
                {
                    if (percent.Value < 0) percent = 0;
                    if (percent.Value > 100) percent = 100;

                    LoginProgressBar.IsIndeterminate = false;
                    LoginProgressBar.Value = percent.Value;
                }
                else
                {
                    LoginProgressBar.IsIndeterminate = true;
                }
            }
            catch
            {
                /* ignore UI update errors */
            }
        }

        private async Task SimulateProgressAsync(int maxPercent, CancellationToken token)
        {
            try
            {
                double v = Math.Min(LoginProgressBar.Value, maxPercent);

                while (!token.IsCancellationRequested && v < maxPercent)
                {
                    v += 1.5;
                    Dispatcher.Invoke(() =>
                    {
                        LoginProgressBar.IsIndeterminate = false;
                        LoginProgressBar.Value = Math.Min(v, maxPercent);
                    });

                    await Task.Delay(90, token);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch
            {
                /* ignore */
            }
        }
    }
}