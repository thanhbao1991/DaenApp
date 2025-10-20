using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.HoaDonViews
{
    public partial class ChiTieuHangNgayEdit : Window
    {
        public ChiTieuHangNgayDto Model { get; set; }
        private readonly IChiTieuHangNgayApi _api;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["ChiTieuHangNgay"];
        private List<NguyenLieuDto> _nguyenLieuList = new();

        public ChiTieuHangNgayEdit(ChiTieuHangNgayDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            TieuDeTextBlock.Text = _friendlyName;
            _api = new ChiTieuHangNgayApi();
            _nguyenLieuList = AppProviders.NguyenLieus.Items.ToList();

            NguyenLieuComboBox.NguyenLieuList = _nguyenLieuList;
            NguyenLieuComboBox.NguyenLieuSelected += nguyenLieu =>
            {
                SoLuongTextBox.Value = 1;
                DonGiaTextBox.Value = nguyenLieu.GiaNhap;
                ThanhTienTextBox.Value = SoLuongTextBox.Value * nguyenLieu.GiaNhap;
            };

            // Đăng ký sự kiện ValueChanged cho SoLuongTextBox và DonGiaTextBox
            SoLuongTextBox.txtValue.ValueChanged += (s, e) =>
            {
                ThanhTienTextBox.Value = SoLuongTextBox.Value * DonGiaTextBox.Value;
            };
            DonGiaTextBox.ValueChanged += (s, e) =>
            {
                ThanhTienTextBox.Value = SoLuongTextBox.Value * DonGiaTextBox.Value;
            };
            Model = dto != null ? dto : new ChiTieuHangNgayDto();

            if (dto != null)
            {
                NguyenLieuComboBox.SetSelectedNguyenLieuByIdWithoutPopup(dto.NguyenLieuId);
                SoLuongTextBox.Value = dto.SoLuong;
                GhiChuTextBox.Text = dto.GhiChu;
                DonGiaTextBox.Value = dto.DonGia;
                ThanhTienTextBox.Value = dto.ThanhTien;
                BillThangCheckBox.IsChecked = dto.BillThang == true ? true : false;
                NgayDatePicker.SelectedDate = dto.Ngay;
            }
            else
            {
                NguyenLieuComboBox.SearchTextBox.Focus();
                NgayDatePicker.SelectedDate = DateTime.Today; // mặc định hôm nay
            }
            if (Model.IsDeleted)
            {
                SaveButton.Content = "Khôi phục";
                SetControlsEnabled(false);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            DonGiaTextBox.IsEnabled = enabled;
            SoLuongTextBox.IsEnabled = enabled;
            ThanhTienTextBox.IsEnabled = enabled;
            NguyenLieuComboBox.IsEnabled = enabled;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                return;
            }

            if (e.Key == Key.Enter)
            {
                if (NguyenLieuComboBox.IsPopupOpen)
                    return;

                SaveButton_Click(SaveButton, new RoutedEventArgs()); // Enter = Lưu & thêm tiếp
            }
        }




        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        public bool KeepAdding { get; private set; } = true; // mặc định như hành vi cũ

        private async Task<bool> SaveAsync()
        {
            ErrorTextBlock.Text = "";

            Model.NguyenLieuId = NguyenLieuComboBox.SelectedNguyenLieu?.Id ?? Guid.Empty;
            if (Model.NguyenLieuId == Guid.Empty || NguyenLieuComboBox.SearchTextBox.Text.Trim() == "")
            {
                ErrorTextBlock.Text = "Nguyên liệu không được để trống.";
                NguyenLieuComboBox.SearchTextBox.Focus();
                return false;
            }

            Model.GhiChu = GhiChuTextBox.Text;
            Model.Ten = NguyenLieuComboBox.SearchTextBox.Text;
            Model.DonGia = DonGiaTextBox.Value;
            Model.SoLuong = SoLuongTextBox.Value;
            Model.ThanhTien = ThanhTienTextBox.Value;
            Model.BillThang = BillThangCheckBox.IsChecked == true;
            Model.Ngay = NgayDatePicker.SelectedDate ?? DateTime.Today;
            Model.NgayGio = Model.Ngay == DateTime.Today ? DateTime.Now : Model.Ngay.AddDays(1).AddMinutes(-1);

            Result<ChiTieuHangNgayDto> result;
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
            KeepAdding = true; // "Lưu & thêm tiếp"
            if (await SaveAsync())
            {
                DialogResult = true;
                Close();
            }
        }

        private async void SaveAndCloseButton_Click(object sender, RoutedEventArgs e)
        {
            KeepAdding = false; // "Lưu & đóng"
            if (await SaveAsync())
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
