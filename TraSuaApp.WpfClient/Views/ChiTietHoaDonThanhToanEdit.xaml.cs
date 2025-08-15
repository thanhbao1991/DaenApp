using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.HoaDonViews
{
    public partial class ChiTietHoaDonThanhToanEdit : Window
    {
        public ChiTietHoaDonThanhToanDto Model { get; set; } = new();

        private readonly IChiTietHoaDonThanhToanApi _api;
        private readonly IPhuongThucThanhToanApi _ptttApi;

        private readonly string _friendlyName = "Chi tiết hóa đơn thanh toán";

        private List<PhuongThucThanhToanDto> _phuongThucThanhToanList = new();

        public ChiTietHoaDonThanhToanEdit(ChiTietHoaDonThanhToanDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _api = new ChiTietHoaDonThanhToanApi();
            _ptttApi = new PhuongThucThanhToanApi();


            _phuongThucThanhToanList = AppProviders.PhuongThucThanhToans.Items.ToList();
            PhuongThucThanhToanComboBox.ItemsSource = _phuongThucThanhToanList;


            if (dto != null)
            {
                Model = dto;
                TenTextBox.Text = dto.Ten;
                LoaiThanhToanTextBox.Text = dto.LoaiThanhToan;
                SoTienTextBox.Value = dto.SoTien;
                SoTienTextBox.Focus();
                PhuongThucThanhToanComboBox.SelectedValue = dto.PhuongThucThanhToanId;
                GhiChuTextBox.Text = dto.GhiChu ?? "";

                if (Model.IsDeleted)
                {
                    TenTextBox.IsEnabled = false;
                    SoTienTextBox.IsEnabled = false;
                    PhuongThucThanhToanComboBox.IsEnabled = false;
                    GhiChuTextBox.IsEnabled = false;
                    SaveButton.Content = "Khôi phục";
                }
            }
            else
            {
                TenTextBox.Focus();
            }
        }



        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            Model.SoTien = 0;
            if (!decimal.TryParse(SoTienTextBox.Text.Replace(",", ""), out var tien))
            {
                ErrorTextBlock.Text = "Số tiền không hợp lệ.";
                SoTienTextBox.Focus();
                return;
            }
            Model.SoTien = tien;

            if (string.IsNullOrWhiteSpace(Model.LoaiThanhToan))
            {
                ErrorTextBlock.Text = "Vui lòng chọn loại thanh toán.";
                return;
            }

            if (PhuongThucThanhToanComboBox.SelectedItem is PhuongThucThanhToanDto pt)
            {
                Model.PhuongThucThanhToanId = pt.Id;
                Model.TenPhuongThucThanhToan = pt.Ten;
            }
            else
            {
                ErrorTextBlock.Text = "Vui lòng chọn phương thức thanh toán.";
                PhuongThucThanhToanComboBox.Focus();
                return;
            }

            Model.GhiChu = GhiChuTextBox.Text.Trim();

            Model.NgayGio = DateTime.Now;

            Result<ChiTietHoaDonThanhToanDto> result;
            if (Model.Id == Guid.Empty)
            {
                result = await _api.CreateAsync(Model);
            }
            else if (Model.IsDeleted)
            {
                result = await _api.RestoreAsync(Model.Id);
            }
            else
            {
                result = await _api.UpdateAsync(Model.Id, Model);
            }

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
                if (Keyboard.FocusedElement is Button) return;

                var request = new TraversalRequest(FocusNavigationDirection.Next);
                if (Keyboard.FocusedElement is UIElement element)
                {
                    element.MoveFocus(request);
                    e.Handled = true;
                }
            }
        }
    }
}