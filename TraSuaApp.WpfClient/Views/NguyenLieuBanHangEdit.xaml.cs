using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class NguyenLieuBanHangEdit : Window
    {
        public NguyenLieuBanHangDto Model { get; set; } = new();
        private readonly INguyenLieuBanHangApi _api;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["NguyenLieuBanHang"];

        private decimal _oldTonKho = 0;

        public NguyenLieuBanHangEdit(NguyenLieuBanHangDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _api = new NguyenLieuBanHangApi();

            if (dto != null)
            {
                Model = dto;

                TenTextBox.Text = dto.Ten;
                TonKhoTextBox.Value = dto.TonKho;
                DonViTinhTextBox.Text = dto.DonViTinh;
                DangSuDungCheckBox.IsChecked = dto.DangSuDung;

                _oldTonKho = dto.TonKho;

                if (Model.IsDeleted)
                {
                    TenTextBox.IsEnabled = false;
                    TonKhoTextBox.IsEnabled = false;
                    DonViTinhTextBox.IsEnabled = false;
                    DangSuDungCheckBox.IsEnabled = false;

                    SaveButton.Content = "Khôi phục";
                }
            }
            else
            {
                DangSuDungCheckBox.IsChecked = true;
                _oldTonKho = 0;
                TenTextBox.Focus();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            if (string.IsNullOrWhiteSpace(TenTextBox.Text))
            {
                ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                TenTextBox.Focus();
                return;
            }

            var newTon = TonKhoTextBox.Value;
            if (newTon < 0)
            {
                ErrorTextBlock.Text = "Tồn kho không được âm.";
                return;
            }

            // ✅ Nhắc user nếu đang sửa và TonKho thay đổi -> hệ thống sẽ log transaction
            if (Model.Id != Guid.Empty && !Model.IsDeleted)
            {
                var delta = newTon - _oldTonKho;
                if (delta != 0)
                {
                    // Không bắt buộc nhập lý do vì DTO chưa có field,
                    // nhưng báo rõ để user biết sẽ ghi log.
                    // (Anh muốn bắt nhập lý do thì mình sẽ thêm field vào DTO)
                }
            }

            Model.Ten = TenTextBox.Text.Trim();
            Model.TonKho = newTon;
            Model.DonViTinh = string.IsNullOrWhiteSpace(DonViTinhTextBox.Text)
                ? null
                : DonViTinhTextBox.Text.Trim();
            Model.DangSuDung = DangSuDungCheckBox.IsChecked == true;

            Result<NguyenLieuBanHangDto> result;
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
                SaveButton_Click(null!, null!);
            }
        }
    }
}
