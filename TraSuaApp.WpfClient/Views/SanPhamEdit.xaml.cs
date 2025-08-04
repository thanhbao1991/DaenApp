using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class SanPhamEdit : Window
    {
        public SanPhamDto Model { get; set; }
        private readonly ISanPhamApi _api;
        private ObservableCollection<SanPhamBienTheDto> _variants;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["SanPham"];

        public SanPhamEdit(SanPhamDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            TieuDeTextBlock.Text = _friendlyName;
            _api = new SanPhamApi();

            // Load nhóm sản phẩm vào ComboBox
            ComboNhom.ItemsSource = AppProviders.NhomSanPhams.Items;
            ComboNhom.DisplayMemberPath = nameof(NhomSanPhamDto.Ten);
            ComboNhom.SelectedValuePath = nameof(NhomSanPhamDto.Id);

            // Khởi tạo Model và biến thể
            Model = dto != null ? dto : new SanPhamDto();
            _variants = dto?.BienThe != null
                ? new ObservableCollection<SanPhamBienTheDto>(dto.BienThe)
                : new ObservableCollection<SanPhamBienTheDto>();
            BienTheListBox.ItemsSource = _variants;

            // Nếu edit mode, gán giá trị lên các control
            if (dto != null)
            {
                TenTextBox.Text = dto.Ten;
                DinhLuongTextBox.Text = dto.DinhLuong;
                VietTatTextBox.Text = dto.VietTat;
                TichDiemCheck.IsChecked = dto.TichDiem;
                NgungBanCheck.IsChecked = dto.NgungBan;
                ComboNhom.SelectedValue = dto.NhomSanPhamId;
            }
            else
            {
                TenTextBox.Focus();
                TichDiemCheck.IsChecked = true;
            }

            // Nếu đang ở trạng thái đã xoá
            if (Model.IsDeleted)
            {
                SaveButton.Content = "Khôi phục";
                SetControlsEnabled(false);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            TenTextBox.IsEnabled = enabled;
            DinhLuongTextBox.IsEnabled = enabled;
            VietTatTextBox.IsEnabled = enabled;
            TichDiemCheck.IsEnabled = enabled;
            NgungBanCheck.IsEnabled = enabled;
            ComboNhom.IsEnabled = enabled;
            BienTheListBox.IsEnabled = enabled;
            BienTheTextBox.IsEnabled = enabled;
            GiaBanTextBox.IsEnabled = enabled;
        }

        private void VariantListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BienTheListBox.SelectedItem is SanPhamBienTheDto v)
            {
                BienTheTextBox.Text = v.TenBienThe;
                GiaBanTextBox.Value = v.GiaBan;
            }
            else
            {
                BienTheTextBox.Clear();
                GiaBanTextBox.Clear();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                return;
            }
            if (e.Key == Key.Enter && !(Keyboard.FocusedElement is Button))
            {
                var req = new TraversalRequest(FocusNavigationDirection.Next);
                if (Keyboard.FocusedElement is UIElement el)
                {
                    el.MoveFocus(req);
                    e.Handled = true;
                }
            }
        }


        private void XoaVariant_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is SanPhamBienTheDto v)
                _variants.Remove(v);
        }

        // Khi check mặc định, bỏ hết còn lại
        private void MacDinhCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is SanPhamBienTheDto sel)
            {
                foreach (var v in _variants)
                    v.MacDinh = (v == sel);
                BienTheListBox.Items.Refresh();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            // Auto-add or update pending variant
            var pName = BienTheTextBox.Text.Trim();
            var pPriceRaw = GiaBanTextBox.Text.Replace(",", "").Trim();
            if (!string.IsNullOrEmpty(pName) || !string.IsNullOrEmpty(pPriceRaw))
            {
                if (string.IsNullOrEmpty(pName))
                {
                    ErrorTextBlock.Text = "Tên biến thể không được để trống.";
                    BienTheTextBox.Focus();
                    return;
                }

                if (!decimal.TryParse(pPriceRaw, out var p) || p < 0)
                {
                    ErrorTextBlock.Text = "Giá bán biến thể không hợp lệ.";
                    GiaBanTextBox.Focus();
                    return;
                }

                if (BienTheListBox.SelectedItem is SanPhamBienTheDto selected)
                {
                    // Cập nhật biến thể đang chọn
                    selected.TenBienThe = pName;
                    selected.GiaBan = p;
                    BienTheListBox.Items.Refresh();
                }
                else
                {
                    // Thêm mới
                    _variants.Add(new SanPhamBienTheDto
                    {
                        Id = Guid.NewGuid(),
                        TenBienThe = pName,
                        GiaBan = p,
                        MacDinh = _variants.Count == 0
                    });
                }

                BienTheListBox.UnselectAll();
                BienTheTextBox.Clear();
                GiaBanTextBox.Clear();
            }

            Model.BienThe = _variants.ToList();

            // Validate chung
            Model.Ten = TenTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(Model.Ten))
            {
                ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                TenTextBox.Focus();
                return;
            }

            if (ComboNhom.SelectedIndex == -1)
            {
                ErrorTextBlock.Text = $"Nhóm {_friendlyName} không được để trống.";
                ComboNhom.Focus();
                return;
            }

            Model.DinhLuong = DinhLuongTextBox.Text.Trim();
            Model.VietTat = VietTatTextBox.Text.Trim();
            Model.NgungBan = NgungBanCheck.IsChecked == true;
            Model.TichDiem = TichDiemCheck.IsChecked == true;
            Model.NhomSanPhamId = ComboNhom.SelectedValue is Guid id ? id : Guid.Empty;

            Result<SanPhamDto> result;
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

        private void VariantListBoxItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is SanPhamBienTheDto d)
            {
                if (BienTheListBox.SelectedItem == d)
                {
                    BienTheListBox.UnselectAll();
                    BienTheTextBox.Clear();
                    GiaBanTextBox.Clear();
                    e.Handled = true;
                }
            }
        }
    }
}
