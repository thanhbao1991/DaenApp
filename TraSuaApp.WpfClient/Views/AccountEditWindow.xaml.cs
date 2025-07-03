using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class AccountEditWindow : Window
    {
        public TaiKhoanDto Account { get; private set; }
        private readonly bool _isEdit;
        private readonly ErrorHandler _errorHandler = new WpfErrorHandler();

        public AccountEditWindow(TaiKhoanDto? taiKhoan = null)
        {
            InitializeComponent();
            _isEdit = taiKhoan != null;
            Account = taiKhoan ?? new TaiKhoanDto();

            LoadForm();

            this.KeyDown += Window_KeyDown; // ⌨️ Bắt phím Enter / Esc
        }

        private void LoadForm()
        {
            TenDangNhapTextBox.Text = Account.TenDangNhap;
            TenHienThiTextBox.Text = Account.TenHienThi;
            IsActiveCheckBox.IsChecked = Account.IsActive;
            MatKhauBox.Password = ""; // không hiển thị mật khẩu cũ

            if (!string.IsNullOrWhiteSpace(Account.VaiTro))
            {
                VaiTroComboBox.SelectedItem = VaiTroComboBox.Items
                    .Cast<ComboBoxItem>()
                    .FirstOrDefault(x => (string)x.Content == Account.VaiTro);
            }

            TenDangNhapTextBox.IsReadOnly = false; // Có thể đổi lại nếu muốn khoá tên đăng nhập khi sửa
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";
            SaveButton.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                if (string.IsNullOrWhiteSpace(TenDangNhapTextBox.Text))
                {
                    ErrorTextBlock.Text = "Tên đăng nhập là bắt buộc.";
                    return;
                }

                if (!_isEdit && string.IsNullOrWhiteSpace(MatKhauBox.Password))
                {
                    ErrorTextBlock.Text = "Mật khẩu là bắt buộc khi thêm mới.";
                    return;
                }

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

                if (response.IsSuccessStatusCode)
                {
                    DialogResult = true;
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    ErrorTextBlock.Text = $"Lỗi {(int)response.StatusCode}: {msg}";
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = $"Lỗi: {ex.Message}";
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
            {
                SaveButton_Click(SaveButton, new RoutedEventArgs());
            }
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }
    }
}