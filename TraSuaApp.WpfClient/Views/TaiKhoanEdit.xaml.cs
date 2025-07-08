using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class TaiKhoanEdit : Window
    {
        public TaiKhoanDto Account { get; private set; }
        private readonly bool _isEdit;
        private readonly WpfErrorHandler _errorHandler;

        public TaiKhoanEdit(TaiKhoanDto? taiKhoan = null)
        {
            InitializeComponent();
            _errorHandler = new WpfErrorHandler(ErrorTextBlock);

            _isEdit = taiKhoan != null;
            Account = taiKhoan ?? new TaiKhoanDto();

            LoadForm();
            this.KeyDown += Window_KeyDown;
        }

        private void LoadForm()
        {
            TenDangNhapTextBox.Text = Account.TenDangNhap;
            TenHienThiTextBox.Text = Account.TenHienThi;
            IsActiveCheckBox.IsChecked = Account.IsActive;
            MatKhauBox.Password = "";

            if (!string.IsNullOrWhiteSpace(Account.VaiTro))
            {
                VaiTroComboBox.SelectedItem = VaiTroComboBox.Items
                    .Cast<ComboBoxItem>()
                    .FirstOrDefault(x => (string)x.Content == Account.VaiTro);
            }

            TenDangNhapTextBox.IsReadOnly = false;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _errorHandler.Clear();
            SaveButton.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                if (string.IsNullOrWhiteSpace(TenDangNhapTextBox.Text))
                    throw new Exception("Tên đăng nhập là bắt buộc.");

                if (!_isEdit && string.IsNullOrWhiteSpace(MatKhauBox.Password))
                    throw new Exception("Mật khẩu là bắt buộc khi thêm mới.");

                Account.TenDangNhap = TenDangNhapTextBox.Text.Trim();
                Account.TenHienThi = TenHienThiTextBox.Text.Trim();
                Account.IsActive = IsActiveCheckBox.IsChecked == true;
                Account.VaiTro = (VaiTroComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();

                if (!string.IsNullOrWhiteSpace(MatKhauBox.Password))
                {
                    Account.MatKhau = MatKhauBox.Password;
                }

                HttpResponseMessage response = _isEdit
                    ? await ApiClient.PutAsync($"/api/taikhoan/{Account.Id}", Account)
                    : await ApiClient.PostAsync("/api/taikhoan", Account);

                if (!response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API lỗi {(int)response.StatusCode}: {msg}");
                }

                var result = await response.Content.ReadFromJsonAsync<Result<TaiKhoanDto>>();
                if (result?.IsSuccess != true)
                    throw new Exception(result?.Message ?? "Không thể lưu tài khoản.");

                DialogResult = true;
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Lưu tài khoản");
            }
            finally
            {
                SaveButton.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SaveButton_Click(SaveButton, new RoutedEventArgs());
            else if (e.Key == Key.Escape)
                DialogResult = false;
        }
    }
}