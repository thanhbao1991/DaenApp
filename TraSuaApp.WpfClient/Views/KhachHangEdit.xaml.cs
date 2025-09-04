using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.SettingsViews
{

    public partial class KhachHangEdit : Window
    {
        public KhachHangDto Model { get; set; } = new();
        private readonly IKhachHangApi _api;
        string _friendlyName = TuDien._tableFriendlyNames["KhachHang"];

        private ObservableCollection<KhachHangPhoneDto> _phones = new();
        private ObservableCollection<KhachHangAddressDto> _addresses = new();

        private KhachHangPhoneDto? _dangSuaPhone;
        private string? _soCuDangSua;

        public KhachHangEdit(KhachHangDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _api = new KhachHangApi();

            if (dto != null)
            {
                Model = dto;
                TenTextBox.Text = dto.Ten;
                DuocNhanVoucherCheckBox.IsChecked = dto.DuocNhanVoucher;
                _phones = new ObservableCollection<KhachHangPhoneDto>(dto.Phones ?? []);
                _addresses = new ObservableCollection<KhachHangAddressDto>(dto.Addresses ?? []);
            }
            else
            {
                Model.DuocNhanVoucher = true;
                DuocNhanVoucherCheckBox.IsChecked = true;
                TenTextBox.Focus();
            }

            PhoneListBox.ItemsSource = _phones;
            DiaChiListBox.ItemsSource = _addresses;

            if (Model.IsDeleted)
            {
                TenTextBox.IsEnabled = false;
                PhoneTextBox.IsEnabled = false;
                DiaChiTextBox.IsEnabled = false;
                SaveButton.Content = "Khôi phục";
                DuocNhanVoucherCheckBox.IsEnabled = true;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            var soDangNhap = PhoneTextBox.Text.Trim();
            var diaChiDangNhap = DiaChiTextBox.Text.Trim();

            Model.Ten = TenTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(Model.Ten))
            {
                ErrorTextBlock.Text = "Tên không được để trống.";
                return;
            }

            Model.DuocNhanVoucher = DuocNhanVoucherCheckBox.IsChecked ?? false;

            var dangSuaTruocKhiThem = _dangSuaPhone;
            var soCu = _soCuDangSua;

            if (!string.IsNullOrWhiteSpace(soDangNhap))
            {
                var soNormalized = soDangNhap.Replace(" ", "").ToLower();
                var trung = _phones.Any(x =>
                    (x.SoDienThoai ?? "").Replace(" ", "").ToLower() == soNormalized &&
                    x != _dangSuaPhone
                );

                if (trung)
                {
                    ErrorTextBlock.Text = "Số điện thoại đang nhập đã tồn tại.";
                    PhoneTextBox.Focus();
                    return;
                }

                ThemPhone();
            }

            if (!string.IsNullOrWhiteSpace(diaChiDangNhap))
            {
                var diaChiTrung = _addresses.Any(x =>
                    string.Equals(x.DiaChi?.Trim(), diaChiDangNhap, StringComparison.OrdinalIgnoreCase));

                if (diaChiTrung)
                {
                    ErrorTextBlock.Text = "Địa chỉ đang nhập đã tồn tại.";
                    DiaChiTextBox.Focus();
                    return;
                }

                ThemDiaChi();
            }

            Model.Phones = _phones.ToList();
            Model.Addresses = _addresses.ToList();

            Result<KhachHangDto> result;
            if (Model.Id == Guid.Empty)
                result = await _api.CreateAsync(Model);
            else if (Model.IsDeleted)
                result = await _api.RestoreAsync(Model.Id);
            else
                result = await _api.UpdateAsync(Model.Id, Model);

            if (!result.IsSuccess)
            {
                ErrorTextBlock.Text = result.Message;
                bool refreshed = false;

                if (result.Message.Contains("điện thoại") && result.Message.Contains($"{_friendlyName} khác"))
                {
                    var duplicate = _phones.FirstOrDefault(x =>
                        string.Equals(x.SoDienThoai?.Trim(), soDangNhap, StringComparison.OrdinalIgnoreCase));

                    if (duplicate != null)
                    {
                        if (dangSuaTruocKhiThem != null && duplicate == dangSuaTruocKhiThem)
                        {
                            if (soCu != null)
                            {
                                duplicate.SoDienThoai = soCu;
                                PhoneTextBox.Text = soCu;
                                PhoneListBox.Items.Refresh();
                                PhoneListBox.UnselectAll();
                                PhoneTextBox.Focus();
                                _dangSuaPhone = null;
                                _soCuDangSua = null;
                                PhoneModeTextBlock.Text = "";
                            }
                        }
                        else
                        {
                            _phones.Remove(duplicate);
                            Model.Phones = _phones.ToList();
                            refreshed = true;
                        }
                    }

                    PhoneTextBox.Text = soDangNhap;
                    PhoneTextBox.Focus();
                    ErrorTextBlock.Text = $"Số điện thoại này đã tồn tại ở {_friendlyName} khác.";
                }

                if (result.Message.Contains("địa chỉ") && result.Message.Contains("trùng"))
                {
                    var duplicate = _addresses.FirstOrDefault(x =>
                        string.Equals(x.DiaChi?.Trim(), diaChiDangNhap, StringComparison.OrdinalIgnoreCase));

                    if (duplicate != null)
                    {
                        _addresses.Remove(duplicate);
                        Model.Addresses = _addresses.ToList();
                        refreshed = true;
                    }

                    DiaChiTextBox.Text = diaChiDangNhap;
                    DiaChiTextBox.Focus();
                    ErrorTextBlock.Text = "Địa chỉ này đã tồn tại.";
                }

                if (refreshed)
                {
                    PhoneListBox.ItemsSource = null;
                    PhoneListBox.ItemsSource = _phones;

                    DiaChiListBox.ItemsSource = null;
                    DiaChiListBox.ItemsSource = _addresses;
                }

                return;
            }

            DialogResult = true;
            Close();
        }

        private void ThemPhone()
        {
            ErrorTextBlock.Text = "";

            var so = PhoneTextBox.Text.Trim();
            if (string.IsNullOrEmpty(so)) return;

            var soNormalized = so.Replace(" ", "").ToLower();

            if (_dangSuaPhone != null)
            {
                var trung = _phones.FirstOrDefault(x =>
                    x != _dangSuaPhone &&
                    (x.SoDienThoai ?? "").Replace(" ", "").ToLower() == soNormalized
                );

                if (trung != null)
                {
                    ErrorTextBlock.Text = "Số điện thoại này đã tồn tại.";
                    return;
                }

                _dangSuaPhone.SoDienThoai = so;
            }
            else
            {
                var existing = _phones.FirstOrDefault(x =>
                    (x.SoDienThoai ?? "").Replace(" ", "").ToLower() == soNormalized
                );

                if (existing != null)
                {
                    ErrorTextBlock.Text = "Số điện thoại này đã tồn tại.";
                    return;
                }

                _phones.Add(new KhachHangPhoneDto
                {
                    Id = Guid.Empty,
                    SoDienThoai = so,
                    IsDefault = _phones.Count == 0
                });
            }

            PhoneTextBox.Clear();
            PhoneListBox.UnselectAll();
            PhoneTextBox.Focus();
            _dangSuaPhone = null;
            _soCuDangSua = null;
            PhoneListBox.Items.Refresh();
            PhoneModeTextBlock.Text = "";
        }

        private void ThemDiaChi()
        {
            var dc = DiaChiTextBox.Text.Trim();
            if (string.IsNullOrEmpty(dc)) return;

            if (DiaChiListBox.SelectedItem is KhachHangAddressDto selected)
            {
                selected.DiaChi = dc;
            }
            else
            {
                var existing = _addresses.FirstOrDefault(x => x.DiaChi == dc);
                if (existing == null)
                {
                    _addresses.Add(new KhachHangAddressDto
                    {
                        Id = Guid.Empty,
                        DiaChi = dc,
                        IsDefault = _addresses.Count == 0
                    });
                }
            }

            DiaChiTextBox.Clear();
            DiaChiListBox.UnselectAll();
            DiaChiTextBox.Focus();
            DiaChiListBox.Items.Refresh();
            DiaChiModeTextBlock.Text = "";
        }

        private void PhoneTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PhoneModeTextBlock.Text = string.IsNullOrWhiteSpace(PhoneTextBox.Text)
                ? ""
                : _dangSuaPhone != null ? "Đang sửa số điện thoại" : "Đang thêm mới...";
        }

        private void DiaChiTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DiaChiModeTextBlock.Text = string.IsNullOrWhiteSpace(DiaChiTextBox.Text)
                ? ""
                : DiaChiListBox.SelectedItem != null ? "Đang sửa địa chỉ" : "Đang thêm mới...";
        }

        private void PhoneListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PhoneListBox.SelectedItem is KhachHangPhoneDto p)
            {
                if (_dangSuaPhone == p)
                {
                    PhoneListBox.UnselectAll();
                    PhoneTextBox.Clear();
                    PhoneTextBox.Focus();
                    _dangSuaPhone = null;
                    _soCuDangSua = null;
                    PhoneModeTextBlock.Text = "";
                }
                else
                {
                    PhoneTextBox.Text = p.SoDienThoai;
                    _dangSuaPhone = p;
                    _soCuDangSua = p.SoDienThoai;
                    PhoneTextBox.Focus();
                    PhoneModeTextBlock.Text = "Đang sửa số điện thoại";
                }
            }
            else
            {
                _dangSuaPhone = null;
                _soCuDangSua = null;
                PhoneModeTextBlock.Text = "";
            }
        }

        private void DiaChiListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DiaChiListBox.SelectedItem is KhachHangAddressDto d)
            {
                if (DiaChiTextBox.Text == d.DiaChi)
                {
                    DiaChiListBox.UnselectAll();
                    DiaChiTextBox.Clear();
                    DiaChiTextBox.Focus();
                    DiaChiModeTextBlock.Text = "";
                }
                else
                {
                    DiaChiTextBox.Text = d.DiaChi;
                    DiaChiTextBox.Focus();
                    DiaChiModeTextBlock.Text = "Đang sửa địa chỉ";
                }
            }
            else
            {
                DiaChiModeTextBlock.Text = "";
            }
        }

        private void XoaDienThoai_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is KhachHangPhoneDto phone)
            {
                _phones.Remove(phone);
                if (_phones.Count == 1)
                    _phones[0].IsDefault = true;

                PhoneListBox.Items.Refresh();
            }
        }

        private void XoaDiaChi_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is KhachHangAddressDto addr)
            {
                _addresses.Remove(addr);
                if (_addresses.Count == 1)
                    _addresses[0].IsDefault = true;

                DiaChiListBox.Items.Refresh();
            }
        }

        private void DienThoaiCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is KhachHangPhoneDto current)
            {
                foreach (var item in _phones)
                    item.IsDefault = item == current;

                PhoneListBox.Items.Refresh();
            }
        }

        private void DiaChiCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is KhachHangAddressDto current)
            {
                foreach (var item in _addresses)
                    item.IsDefault = item == current;

                DiaChiListBox.Items.Refresh();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CloseButton_Click(null!, null!);
            else
            if (e.Key == Key.Enter)
            {
                SaveButton_Click(null, null);

            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void PhoneListBoxItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is KhachHangPhoneDto p)
            {
                if (PhoneListBox.SelectedItem == p)
                {
                    PhoneListBox.UnselectAll();
                    PhoneTextBox.Clear();
                    PhoneTextBox.Focus();
                    PhoneModeTextBlock.Text = "Nhập để thêm mới";

                    _dangSuaPhone = null;
                    _soCuDangSua = null;
                    e.Handled = true;
                }
            }
        }

        private void DiaChiListBoxItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item && item.DataContext is KhachHangAddressDto d)
            {
                if (DiaChiListBox.SelectedItem == d)
                {
                    DiaChiListBox.UnselectAll();
                    DiaChiTextBox.Clear();
                    DiaChiTextBox.Focus();
                    DiaChiModeTextBlock.Text = "Nhập để thêm mới";

                    e.Handled = true;
                }
            }
        }

        private void DiaChiTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                if (DiaChiListBox.SelectedIndex < DiaChiListBox.Items.Count - 1)
                {
                    DiaChiListBox.SelectedIndex++;
                    DiaChiListBox.ScrollIntoView(DiaChiListBox.SelectedItem);
                    DiaChiTextBox.SelectAll();
                    e.Handled = true;

                }
            }
            else
            if (e.Key == Key.Up)
            {
                if (DiaChiListBox.SelectedIndex > 0)
                {
                    DiaChiListBox.SelectedIndex--;
                    DiaChiListBox.ScrollIntoView(DiaChiListBox.SelectedItem);
                    DiaChiTextBox.SelectAll();
                    e.Handled = true;
                }
            }
        }
    }
}
