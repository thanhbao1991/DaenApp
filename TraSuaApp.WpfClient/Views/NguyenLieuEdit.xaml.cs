using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.SettingsViews
{
    public partial class NguyenLieuEdit : Window
    {
        public NguyenLieuDto Model { get; set; } = new();

        private readonly INguyenLieuApi _api;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["NguyenLieu"];

        private List<NguyenLieuBanHangDto> _nguyenLieuBanHangList = new();

        public NguyenLieuEdit(NguyenLieuDto? dto = null)
        {
            InitializeComponent();

            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _api = new NguyenLieuApi();

            // Load list NguyenLieuBanHang để search box dùng
            if (AppProviders.NguyenLieuBanHangs?.Items != null)
                _nguyenLieuBanHangList = AppProviders.NguyenLieuBanHangs.Items.ToList();

            NguyenLieuBanHangSearchBox.NguyenLieuBanHangList = _nguyenLieuBanHangList;

            NguyenLieuBanHangSearchBox.NguyenLieuBanHangSelected += nlbh =>
            {
                // Khi chọn NL bán, gán vào model
                Model.NguyenLieuBanHangId = nlbh.Id;

                // Nếu anh chưa nhập hệ số, set default = 1 cho dễ
                if (HeSoQuyDoiNumeric.Value <= 0)
                    HeSoQuyDoiNumeric.Value = 1;
            };

            NguyenLieuBanHangSearchBox.NguyenLieuBanHangCleared += () =>
            {
                Model.NguyenLieuBanHangId = null;
                // Khi không map nữa thì hệ số cũng cho null (tui để Value = 0 để biểu diễn “chưa có”)
                HeSoQuyDoiNumeric.Value = 0;
            };

            if (dto != null)
            {
                Model = dto;

                TenTextBox.Text = dto.Ten ?? "";
                DonViTinhTextBox.Text = dto.DonViTinh ?? "";
                GiaNhapTextBox.Text = dto.GiaNhap.ToString("0.##");
                DangSuDungCheckBox.IsChecked = dto.DangSuDung;

                // Map NL bán hàng + hệ số
                if (dto.NguyenLieuBanHangId.HasValue)
                    NguyenLieuBanHangSearchBox.SetSelectedNguyenLieuBanHangByIdWithoutPopup(dto.NguyenLieuBanHangId.Value);

                HeSoQuyDoiNumeric.Value = dto.HeSoQuyDoiBanHang ?? 0;

                // Nếu đã bị xoá mềm
                if (Model.IsDeleted)
                {
                    TenTextBox.IsEnabled = false;
                    DonViTinhTextBox.IsEnabled = false;
                    GiaNhapTextBox.IsEnabled = false;
                    NguyenLieuBanHangSearchBox.IsEnabled = false;
                    HeSoQuyDoiNumeric.IsEnabled = false;
                    DangSuDungCheckBox.IsEnabled = false;

                    SaveButton.Content = "Khôi phục";
                }
            }
            else
            {
                // Default
                DangSuDungCheckBox.IsChecked = true;
                GiaNhapTextBox.Text = "0";
                HeSoQuyDoiNumeric.Value = 0;

                TenTextBox.Focus();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            // 1) Tên
            Model.Ten = (TenTextBox.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(Model.Ten))
            {
                ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                TenTextBox.Focus();
                return;
            }

            // 2) Đơn vị nhập
            Model.DonViTinh = string.IsNullOrWhiteSpace(DonViTinhTextBox.Text)
                ? null
                : DonViTinhTextBox.Text.Trim();

            // 3) Giá nhập
            decimal giaNhap = 0;
            if (!string.IsNullOrWhiteSpace(GiaNhapTextBox.Text))
            {
                if (!decimal.TryParse(GiaNhapTextBox.Text.Trim(), out giaNhap))
                {
                    ErrorTextBlock.Text = "Giá nhập phải là số.";
                    GiaNhapTextBox.Focus();
                    return;
                }
            }
            Model.GiaNhap = giaNhap;

            // 4) Đang sử dụng (UI đang ghi “Ẩn” nhưng anh đang dùng biến DangSuDung – giữ nguyên logic hiện tại)
            Model.DangSuDung = DangSuDungCheckBox.IsChecked == true;

            // 5) Map sang NL bán + hệ số
            var selectedNlbh = NguyenLieuBanHangSearchBox.SelectedNguyenLieuBanHang;
            Model.NguyenLieuBanHangId = selectedNlbh?.Id;

            // Nếu có map NL bán thì bắt buộc hệ số > 0
            if (Model.NguyenLieuBanHangId.HasValue)
            {
                if (HeSoQuyDoiNumeric.Value <= 0)
                {
                    ErrorTextBlock.Text = "Hệ số quy đổi phải lớn hơn 0 khi đã chọn nguyên liệu bán hàng.";
                    HeSoQuyDoiNumeric.Focus();
                    return;
                }
                Model.HeSoQuyDoiBanHang = HeSoQuyDoiNumeric.Value;
            }
            else
            {
                // Không map -> không lưu hệ số
                Model.HeSoQuyDoiBanHang = null;
            }

            Result<NguyenLieuDto> result;
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
                // tránh Enter khi popup searchbox đang mở (giống pattern các form khác)
                if (NguyenLieuBanHangSearchBox.IsPopupOpen)
                    return;

                SaveButton_Click(null!, null!);
            }
        }
    }
}