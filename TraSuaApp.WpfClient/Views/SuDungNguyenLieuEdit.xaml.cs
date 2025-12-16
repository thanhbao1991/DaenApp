using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Controls;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class SuDungNguyenLieuEdit : Window
    {
        public SuDungNguyenLieuDto Model { get; set; }

        private readonly ISuDungNguyenLieuApi _api;
        private readonly CongThucDto _parentCongThuc;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["SuDungNguyenLieu"];

        private List<NguyenLieuBanHangDto> _nguyenLieuBanHangList = new();

        public bool KeepAdding { get; private set; } = true;

        public SuDungNguyenLieuEdit(CongThucDto congThuc, SuDungNguyenLieuDto? dto = null)
        {
            InitializeComponent();
            KeyDown += Window_KeyDown;

            _api = new SuDungNguyenLieuApi();
            _parentCongThuc = congThuc;

            Title = _friendlyName;
            TieuDeTextBlock.Text = $"Công thức: {congThuc.TenSanPham} - {congThuc.TenBienThe}";

            // Nạp list nguyên liệu bán hàng
            if (AppProviders.NguyenLieuBanHangs?.Items != null)
                _nguyenLieuBanHangList = AppProviders.NguyenLieuBanHangs.Items.ToList();

            NguyenLieuBanHangSearchBox.NguyenLieuBanHangList = _nguyenLieuBanHangList;

            NguyenLieuBanHangSearchBox.NguyenLieuBanHangSelected += nl =>
            {
                if (SoLuongNumeric.Value <= 0)
                    SoLuongNumeric.Value = 1;
            };

            Model = dto ?? new SuDungNguyenLieuDto
            {
                CongThucId = congThuc.Id,
                SoLuong = 1
            };

            if (dto != null)
            {
                // Model.NguyenLieuId lúc này được hiểu là Id của NguyenLieuBanHang
                NguyenLieuBanHangSearchBox.SetSelectedNguyenLieuBanHangByIdWithoutPopup(dto.NguyenLieuId);
                SoLuongNumeric.Value = dto.SoLuong;
            }
            else
            {
                SoLuongNumeric.Value = Model.SoLuong;
                NguyenLieuBanHangSearchBox.SearchTextBox.Focus();
            }

            if (Model.IsDeleted)
            {
                SaveButton.Content = "Khôi phục";
                SetControlsEnabled(false);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            NguyenLieuBanHangSearchBox.IsEnabled = enabled;
            SoLuongNumeric.IsEnabled = enabled;
        }

        private async Task<bool> SaveAsync()
        {
            ErrorTextBlock.Text = "";

            // luôn gán công thức cha
            Model.CongThucId = _parentCongThuc.Id;

            var selectedNl = NguyenLieuBanHangSearchBox.SelectedNguyenLieuBanHang;
            // 🟟 Model.NguyenLieuId bây giờ là Id của NguyenLieuBanHang
            Model.NguyenLieuId = selectedNl?.Id ?? Guid.Empty;

            if (Model.NguyenLieuId == Guid.Empty ||
                string.IsNullOrWhiteSpace(NguyenLieuBanHangSearchBox.SearchTextBox.Text))
            {
                ErrorTextBlock.Text = "Nguyên liệu bán hàng không được để trống.";
                NguyenLieuBanHangSearchBox.SearchTextBox.Focus();
                return false;
            }

            if (SoLuongNumeric.Value <= 0)
            {
                ErrorTextBlock.Text = "Số lượng phải lớn hơn 0.";
                SoLuongNumeric.Focus();
                return false;
            }

            Model.SoLuong = SoLuongNumeric.Value;

            Result<SuDungNguyenLieuDto> result;
            if (Model.Id == Guid.Empty)
                result = await _api.CreateAsync(Model);
            else if (Model.IsDeleted)
                result = await _api.RestoreAsync(Model.Id);
            else
                result = await _api.UpdateAsync(Model.Id, Model);

            if (!result.IsSuccess)
            {
                ErrorTextBlock.Text = result.Message;
                return false;
            }

            return true;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            KeepAdding = true; // Lưu & tiếp
            if (await SaveAsync())
            {
                DialogResult = true;
                Close();
            }
        }

        private async void SaveAndCloseButton_Click(object sender, RoutedEventArgs e)
        {
            KeepAdding = false; // Lưu & đóng
            if (await SaveAsync())
            {
                DialogResult = true;
                Close();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                return;
            }

            if (e.Key == Key.Enter)
            {
                if (NguyenLieuBanHangSearchBox.IsPopupOpen)
                    return;

                SaveButton_Click(SaveButton, new RoutedEventArgs());
            }
        }
    }
}