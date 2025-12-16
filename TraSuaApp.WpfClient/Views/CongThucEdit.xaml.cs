using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class CongThucEdit : Window
    {
        public CongThucDto Model { get; set; } = new();

        private readonly ICongThucApi _api;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["CongThuc"];

        private readonly List<SanPhamDto> _sanPhamList = new();

        public bool KeepAdding { get; private set; } = true;

        public CongThucEdit(CongThucDto? dto = null)
        {
            InitializeComponent();
            KeyDown += Window_KeyDown;

            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _api = new CongThucApi();

            // load list sản phẩm
            if (AppProviders.SanPhams?.Items != null)
                _sanPhamList = AppProviders.SanPhams.Items.ToList();

            SanPhamSearchBox.SanPhamList = _sanPhamList;

            SanPhamSearchBox.SanPhamBienTheSelected += (sp, bt) =>
            {
                if (bt != null)
                {
                    Model.SanPhamBienTheId = bt.Id;
                    Model.TenSanPham = sp.Ten;
                    Model.TenBienThe = bt.TenBienThe;
                }
                else
                {
                    Model.SanPhamBienTheId = Guid.Empty;
                }
            };

            if (dto != null)
            {
                Model = dto;

                // set lại selection theo SanPhamBienTheId
                var sp = _sanPhamList.FirstOrDefault(
                    s => s.BienThe.Any(bt => bt.Id == dto.SanPhamBienTheId));

                var bt = sp?.BienThe.FirstOrDefault(b => b.Id == dto.SanPhamBienTheId);

                if (sp != null && bt != null)
                    SanPhamSearchBox.SetSelectedSanPham(sp, bt);
            }
            else
            {
                // focus ô search
                SanPhamSearchBox.SearchTextBox.Focus();
            }

            if (Model.IsDeleted)
            {
                SaveButton.Content = "Khôi phục";
                SetControlsEnabled(false);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            SanPhamSearchBox.IsEnabled = enabled;
        }

        private async Task<bool> SaveAsync()
        {
            ErrorTextBlock.Text = "";

            if (Model.SanPhamBienTheId == Guid.Empty ||
                SanPhamSearchBox.SelectedSanPham == null ||
                SanPhamSearchBox.SelectedBienThe == null)
            {
                ErrorTextBlock.Text = "Vui lòng chọn sản phẩm / biến thể.";
                SanPhamSearchBox.SearchTextBox.Focus();
                return false;
            }

            // cập nhật lại tên SP / BT từ selection để chắc ăn
            Model.TenSanPham = SanPhamSearchBox.SelectedSanPham!.Ten;
            Model.TenBienThe = SanPhamSearchBox.SelectedBienThe!.TenBienThe;

            Result<CongThucDto> result;
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
            KeepAdding = true; // Lưu và thêm tiếp
            if (await SaveAsync())
            {
                DialogResult = true;
                Close();
            }
        }

        private async void SaveAndCloseButton_Click(object sender, RoutedEventArgs e)
        {
            KeepAdding = false; // Lưu và đóng
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
                if (SanPhamSearchBox.IsPopupOpen)
                    return;

                SaveButton_Click(SaveButton, new RoutedEventArgs());
            }
        }
    }
}