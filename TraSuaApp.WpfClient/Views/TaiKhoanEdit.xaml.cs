using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class TaiKhoanEdit : Window
    {
        public TaiKhoanDto Model { get; set; } = new();
        private readonly ITaiKhoanApi _api;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["TaiKhoan"];

        public TaiKhoanEdit(TaiKhoanDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _api = new TaiKhoanApi();

            if (dto != null)
            {
                Model = dto;
                TenDangNhapTextBox.Text = dto.TenDangNhap;
                TenHienThiTextBox.Text = dto.TenHienThi;
                // Chọn ComboBoxItem matching
                foreach (ComboBoxItem it in VaiTroComboBox.Items)
                    if ((string)it.Content == dto.VaiTro)
                        VaiTroComboBox.SelectedItem = it;
                IsActiveCheckBox.IsChecked = dto.IsActive;
            }
            else
            {
                // mặc định chọn "Nhân viên"
                VaiTroComboBox.SelectedIndex = 1;
                IsActiveCheckBox.IsChecked = true;
                TenDangNhapTextBox.Focus();
            }


            if (Model.IsDeleted)
            {
                TenDangNhapTextBox.IsEnabled = false;
                SaveButton.Content = "Khôi phục";
            }

        }



        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            Model.TenDangNhap = TenDangNhapTextBox.Text.Trim();
            Model.TenHienThi = TenHienThiTextBox.Text.Trim();
            Model.IsActive = IsActiveCheckBox.IsChecked == true;

            if (VaiTroComboBox.SelectedItem is ComboBoxItem sel)
                Model.VaiTro = sel.Content.ToString()!;

            var pwd = MatKhauBox.Password.Trim();
            if (!string.IsNullOrEmpty(pwd))
                Model.MatKhau = pwd;

            if (string.IsNullOrWhiteSpace(Model.TenDangNhap))
            {
                ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                TenDangNhapTextBox.Focus();
                return;
            }

            Result<TaiKhoanDto> result;
            if (Model.Id == Guid.Empty)
                result = await _api.CreateAsync(Model);
            else if (Model.IsDeleted)
                result = await _api.RestoreAsync(Model.Id);
            else
                result = await _api.UpdateAsync(Model.Id, Model);

            if (!result.IsSuccess)
            {
                ErrorTextBlock.Text = result.Message;
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseButton_Click(null!, null!);
                return;
            }

            if (e.Key == Key.Enter)
            {
                SaveButton_Click(null, null);

            }
        }

        // Giữ event handler để tránh cảnh báo XAML
        private void NhomSanPhamListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}
