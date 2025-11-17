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
        public ObservableCollection<KhachHangPhoneDto> Phones { get; set; } = new();
        public ObservableCollection<KhachHangAddressDto> Addresses { get; set; } = new();

        private readonly IKhachHangApi _api;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["KhachHang"];

        public KhachHangEdit(KhachHangDto? dto = null)
        {
            // Chuẩn bị dữ liệu trước
            if (dto != null)
            {
                Model = dto;

                Phones = new ObservableCollection<KhachHangPhoneDto>(
                    dto.Phones.Select(p => new KhachHangPhoneDto
                    {
                        Id = p.Id,
                        KhachHangId = p.KhachHangId,
                        SoDienThoai = p.SoDienThoai,
                        IsDefault = p.IsDefault
                    }));

                Addresses = new ObservableCollection<KhachHangAddressDto>(
                    dto.Addresses.Select(a => new KhachHangAddressDto
                    {
                        Id = a.Id,
                        KhachHangId = a.KhachHangId,
                        DiaChi = a.DiaChi,
                        IsDefault = a.IsDefault
                    }));

                if (Phones.Count == 0)
                    Phones.Add(new KhachHangPhoneDto { IsDefault = true });

                if (Addresses.Count == 0)
                    Addresses.Add(new KhachHangAddressDto { IsDefault = true });
            }
            else
            {
                // Mặc định khi thêm mới
                Model = new KhachHangDto
                {
                    DuocNhanVoucher = true
                };

                Phones = new ObservableCollection<KhachHangPhoneDto>
                {
                    new KhachHangPhoneDto { IsDefault = true }
                };
                Addresses = new ObservableCollection<KhachHangAddressDto>
                {
                    new KhachHangAddressDto { IsDefault = true }
                };
            }

            InitializeComponent();

            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _api = new KhachHangApi();

            // Binding
            DataContext = this;

            // Nếu đang ở trạng thái Deleted thì chỉ cho khôi phục
            if (Model.IsDeleted)
            {
                TenTextBox.IsEnabled = false;
                DuocNhanVoucherCheckBox.IsEnabled = false;

                AddPhoneButton.IsEnabled = false;
                AddAddressButton.IsEnabled = false;
                SaveButton.Content = "Khôi phục";
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            if (!Model.IsDeleted)
            {
                // Bắt lỗi tên trống (nếu không muốn bắt thì bỏ đoạn này)
                if (string.IsNullOrWhiteSpace(TenTextBox.Text))
                {
                    ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                    TenTextBox.Focus();
                    return;
                }
            }

            // Cập nhật Model từ UI
            Model.Ten = (TenTextBox.Text ?? string.Empty).Trim();
            Model.DuocNhanVoucher = DuocNhanVoucherCheckBox.IsChecked == true;

            // Đồng bộ phones/addresses (lọc rỗng)
            var phoneList = Phones
                .Where(p => !string.IsNullOrWhiteSpace(p.SoDienThoai))
                .ToList();
            var addressList = Addresses
                .Where(a => !string.IsNullOrWhiteSpace(a.DiaChi))
                .ToList();

            if (!Model.IsDeleted)
            {
                if (phoneList.Count == 0)
                {
                    ErrorTextBlock.Text = "Vui lòng nhập ít nhất một số điện thoại.";
                    return;
                }

                if (addressList.Count == 0)
                {
                    ErrorTextBlock.Text = "Vui lòng nhập ít nhất một địa chỉ.";
                    return;
                }
            }

            // Đảm bảo luôn có 1 default (UI cố gắng làm rồi, nhưng đảm bảo thêm)
            EnsureOneDefault(phoneList);
            EnsureOneDefault(addressList);

            Model.Phones = phoneList
                .Select(p => new KhachHangPhoneDto
                {
                    Id = p.Id,
                    KhachHangId = Model.Id,
                    SoDienThoai = p.SoDienThoai?.Trim() ?? "",
                    IsDefault = p.IsDefault
                })
                .ToList();

            Model.Addresses = addressList
                .Select(a => new KhachHangAddressDto
                {
                    Id = a.Id,
                    KhachHangId = Model.Id,
                    DiaChi = a.DiaChi?.Trim() ?? "",
                    IsDefault = a.IsDefault
                })
                .ToList();

            Result<KhachHangDto> result;
            if (Model.Id == Guid.Empty)
            {
                // Thêm mới
                result = await _api.CreateAsync(Model);
            }
            else if (Model.IsDeleted)
            {
                // Khôi phục
                result = await _api.RestoreAsync(Model.Id);
            }
            else
            {
                // Cập nhật
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

        private void EnsureOneDefault<T>(IList<T> list) where T : class
        {
            if (list.Count == 0) return;

            // dynamic một tí cho nhanh
            var defaults = list
                .Where(x => (bool)(x.GetType().GetProperty("IsDefault")?.GetValue(x)!))
                .ToList();

            if (defaults.Count == 0)
            {
                // Không có default -> set phần tử cuối cùng
                var last = list[^1];
                last.GetType().GetProperty("IsDefault")?.SetValue(last, true);
            }
            else if (defaults.Count > 1)
            {
                // Nhiều default -> chỉ giữ lại cái cuối cùng
                for (int i = 0; i < defaults.Count - 1; i++)
                    defaults[i].GetType().GetProperty("IsDefault")?.SetValue(defaults[i], false);
            }
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

        // ====== PHONE ======
        private void AddPhoneButton_Click(object sender, RoutedEventArgs e)
        {
            bool any = Phones.Any();
            Phones.Add(new KhachHangPhoneDto
            {
                IsDefault = !any
            });
        }

        private void DeletePhoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not KhachHangPhoneDto dto) return;

            bool wasDefault = dto.IsDefault;
            Phones.Remove(dto);

            if (wasDefault && Phones.Count > 0 && !Phones.Any(x => x.IsDefault))
            {
                Phones[0].IsDefault = true;
            }
        }


        // ====== ADDRESS ======
        private void AddAddressButton_Click(object sender, RoutedEventArgs e)
        {
            bool any = Addresses.Any();
            Addresses.Add(new KhachHangAddressDto
            {
                IsDefault = !any
            });
        }

        private void DeleteAddressButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.DataContext is not KhachHangAddressDto dto) return;

            bool wasDefault = dto.IsDefault;
            Addresses.Remove(dto);

            if (wasDefault && Addresses.Count > 0 && !Addresses.Any(x => x.IsDefault))
            {
                Addresses[0].IsDefault = true;
            }
        }

        // ====== PHONE ======
        private void PhoneDefaultCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb) return;
            if (cb.DataContext is not KhachHangPhoneDto current) return;

            // Duyệt qua ObservableCollection Phones
            foreach (var p in Phones)
            {
                if (!ReferenceEquals(p, current))
                    p.IsDefault = false;
            }

            // 🟟 BẮT BUỘC: refresh lại UI để checkbox khác tắt
            PhonesItemsControl.Items.Refresh();
        }

        // ====== ADDRESS ======
        private void AddressDefaultCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb) return;
            if (cb.DataContext is not KhachHangAddressDto current) return;

            // Duyệt qua ObservableCollection Addresses
            foreach (var a in Addresses)
            {
                if (!ReferenceEquals(a, current))
                    a.IsDefault = false;
            }

            // 🟟 BẮT BUỘC: refresh lại UI
            AddressesItemsControl.Items.Refresh();
        }


    }
}
