using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Config;
using TraSuaApp.WpfClient.DataProviders;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.SettingsViews
{
    public partial class KhachHangList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["KhachHang"];

        private Guid? _editingId;
        private bool _editingIsDeleted;

        public KhachHangDto Model { get; set; } = new();
        public ObservableCollection<KhachHangPhoneDto> Phones { get; set; } = new();
        public ObservableCollection<KhachHangAddressDto> Addresses { get; set; } = new();

        public KhachHangList()
        {
            InitializeComponent();

            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            PreviewKeyDown += KhachHangList_PreviewKeyDown;
            Closed += KhachHangList_Closed;

            DataContext = this;

            _viewSource.Filter += ViewSource_Filter;
            BindSource();

            AppProviders.KhachHangs.OnChanged += AppProviders_KhachHangs_OnChanged;

            Loaded += async (_, __) =>
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    await AppProviders.KhachHangs.ReloadAsync();
                    BindSource();
                    ApplySearch();
                    SetAddMode();
                }
                catch (Exception ex)
                {
                    _errorHandler.Handle(ex, "Load");
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            };
        }

        private void KhachHangList_Closed(object? sender, EventArgs e)
        {
            AppProviders.KhachHangs.OnChanged -= AppProviders_KhachHangs_OnChanged;
        }

        private void AppProviders_KhachHangs_OnChanged()
        {
            BindSource();
            ApplySearch();
        }

        private void BindSource()
        {
            _viewSource.Source = AppProviders.KhachHangs.Items;
            KhachHangDataGrid.ItemsSource = _viewSource.View;
        }

        private void ApplySearch()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplySearch);
                return;
            }

            if (_viewSource.View == null)
                return;

            _viewSource.View.Refresh();

            _viewSource.View.SortDescriptions.Clear();
            _viewSource.View.SortDescriptions.Add(
                new SortDescription(nameof(KhachHangDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<KhachHangDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not KhachHangDto item)
            {
                e.Accepted = false;
                return;
            }

            var keyword = StringHelper.MyNormalizeText(SearchTextBox.Text.Trim());
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }

        private void SetAddMode()
        {
            _editingId = null;
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

            PhonesItemsControl.ItemsSource = Phones;
            AddressesItemsControl.ItemsSource = Addresses;

            FormTitleTextBlock.Text = "Thêm khách hàng";
            SelectedInfoTextBlock.Text = "Nhập thông tin khách hàng bên dưới.";
            SaveButton.Content = "Lưu";

            TenTextBox.Text = string.Empty;
            DuocNhanVoucherCheckBox.IsChecked = true;
            ErrorTextBlock.Text = string.Empty;

            TenTextBox.IsEnabled = true;
            DuocNhanVoucherCheckBox.IsEnabled = true;
            AddPhoneButton.IsEnabled = true;
            AddAddressButton.IsEnabled = true;
            SaveButton.IsEnabled = true;

            KhachHangDataGrid.UnselectAll();
            TenTextBox.Focus();
        }

        private void SetEditMode(KhachHangDto selected)
        {
            _editingId = selected.Id;

            Model = new KhachHangDto
            {
                Id = selected.Id,
                Ten = selected.Ten,
                DuocNhanVoucher = selected.DuocNhanVoucher,

                LastModified = selected.LastModified
            };

            Phones = new ObservableCollection<KhachHangPhoneDto>(
                selected.Phones?.Select(p => new KhachHangPhoneDto
                {
                    Id = p.Id,
                    KhachHangId = p.KhachHangId,
                    SoDienThoai = p.SoDienThoai,
                    IsDefault = p.IsDefault
                }) ?? Enumerable.Empty<KhachHangPhoneDto>());

            Addresses = new ObservableCollection<KhachHangAddressDto>(
                selected.Addresses?.Select(a => new KhachHangAddressDto
                {
                    Id = a.Id,
                    KhachHangId = a.KhachHangId,
                    DiaChi = a.DiaChi,
                    IsDefault = a.IsDefault
                }) ?? Enumerable.Empty<KhachHangAddressDto>());

            if (Phones.Count == 0)
                Phones.Add(new KhachHangPhoneDto { IsDefault = true });

            if (Addresses.Count == 0)
                Addresses.Add(new KhachHangAddressDto { IsDefault = true });

            PhonesItemsControl.ItemsSource = Phones;
            AddressesItemsControl.ItemsSource = Addresses;

            SelectedInfoTextBlock.Text = selected.Ten ?? "";


            TenTextBox.Text = selected.Ten;
            DuocNhanVoucherCheckBox.IsChecked = selected.DuocNhanVoucher;
            ErrorTextBlock.Text = string.Empty;

            SaveButton.IsEnabled = true;

            TenTextBox.Focus();
            TenTextBox.SelectAll();
        }

        private int FindIndex(Guid id)
        {
            for (int i = 0; i < AppProviders.KhachHangs.Items.Count; i++)
            {
                if (AppProviders.KhachHangs.Items[i].Id == id)
                    return i;
            }

            return -1;
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                await AppProviders.KhachHangs.ReloadAsync();
                BindSource();
                ApplySearch();
                SetAddMode();
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Reload");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            SetAddMode();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (KhachHangDataGrid.SelectedItem is not KhachHangDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetEditMode(selected);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = string.Empty;

            var ten = TenTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(ten))
            {
                ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                TenTextBox.Focus();
                return;
            }

            var phoneList = Phones.Where(p => !string.IsNullOrWhiteSpace(p.SoDienThoai)).ToList();
            var addressList = Addresses.Where(a => !string.IsNullOrWhiteSpace(a.DiaChi)).ToList();

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

            EnsureOneDefaultPhone();
            EnsureOneDefaultAddress();

            Model.Ten = ten;
            Model.DuocNhanVoucher = DuocNhanVoucherCheckBox.IsChecked == true;

            Model.Phones = phoneList.Select(p => new KhachHangPhoneDto
            {
                Id = p.Id,
                KhachHangId = Model.Id,
                SoDienThoai = p.SoDienThoai?.Trim() ?? "",
                IsDefault = p.IsDefault
            }).ToList();

            Model.Addresses = addressList.Select(a => new KhachHangAddressDto
            {
                Id = a.Id,
                KhachHangId = Model.Id,
                DiaChi = a.DiaChi?.Trim() ?? "",
                IsDefault = a.IsDefault
            }).ToList();

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                Result<KhachHangDto> result;

                if (Model.Id == Guid.Empty)
                {
                    result = await Apis.KhachHang.CreateAsync(Model);
                }
                else
                {
                    result = await Apis.KhachHang.UpdateAsync(Model.Id, Model);
                }

                if (!result.IsSuccess)
                {
                    ErrorTextBlock.Text = result.Message;
                    return;
                }

                if (result.Data != null)
                {
                    var index = FindIndex(result.Data.Id);

                    if (index >= 0)
                        AppProviders.KhachHangs.Items[index] = result.Data;
                    else
                        AppProviders.KhachHangs.Items.Add(result.Data);
                }
                else
                {
                    await AppProviders.KhachHangs.ReloadAsync();
                }

                NotiHelper.ShowSuccess(result.Message);
                BindSource();
                ApplySearch();
                SetAddMode();
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Save");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (KhachHangDataGrid.SelectedItem is not KhachHangDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} '{selected.Ten}'?",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response = await ApiClient.DeleteAsync($"/api/khachhang/{selected.Id}");

                if (!response.IsSuccessStatusCode)
                {
                    NotiHelper.ShowError($"Xoá thất bại ({(int)response.StatusCode}).");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<KhachHangDto>>();

                var id = result?.Data?.Id != Guid.Empty ? result!.Data!.Id : selected.Id;
                var index = FindIndex(id);

                if (index >= 0)
                    AppProviders.KhachHangs.Items.RemoveAt(index);
                else
                    await AppProviders.KhachHangs.ReloadAsync();

                NotiHelper.ShowSuccess(result?.Message ?? "Xoá thành công.");
                BindSource();
                ApplySearch();
                SetAddMode();
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Delete");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void KhachHangDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (KhachHangDataGrid.SelectedItem is not KhachHangDto selected)
                return;

            SetEditMode(selected);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddPhoneButton_Click(object sender, RoutedEventArgs e)
        {
            Phones.Add(new KhachHangPhoneDto
            {
                IsDefault = Phones.Count == 0
            });
        }

        private void DeletePhoneButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not KhachHangPhoneDto dto)
                return;

            var wasDefault = dto.IsDefault;
            Phones.Remove(dto);

            if (wasDefault && Phones.Count > 0 && !Phones.Any(x => x.IsDefault))
                Phones[0].IsDefault = true;
        }

        private void AddAddressButton_Click(object sender, RoutedEventArgs e)
        {
            Addresses.Add(new KhachHangAddressDto
            {
                IsDefault = Addresses.Count == 0
            });
        }

        private void DeleteAddressButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not KhachHangAddressDto dto)
                return;

            var wasDefault = dto.IsDefault;
            Addresses.Remove(dto);

            if (wasDefault && Addresses.Count > 0 && !Addresses.Any(x => x.IsDefault))
                Addresses[0].IsDefault = true;
        }

        private void PhoneDefaultCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb) return;
            if (cb.DataContext is not KhachHangPhoneDto current) return;

            foreach (var p in Phones)
            {
                if (!ReferenceEquals(p, current))
                    p.IsDefault = false;
            }

            PhonesItemsControl.Items.Refresh();
        }

        private void AddressDefaultCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb) return;
            if (cb.DataContext is not KhachHangAddressDto current) return;

            foreach (var a in Addresses)
            {
                if (!ReferenceEquals(a, current))
                    a.IsDefault = false;
            }

            AddressesItemsControl.Items.Refresh();
        }

        private void KhachHangList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            {
                AddButton_Click(null!, null!);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            {
                EditButton_Click(null!, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                DeleteButton_Click(null!, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                ReloadButton_Click(null!, null!);
                e.Handled = true;
            }
        }

        private void EnsureOneDefaultPhone()
        {
            if (Phones.Count == 0) return;

            if (!Phones.Any(x => x.IsDefault))
                Phones[0].IsDefault = true;

            var found = false;
            foreach (var p in Phones)
            {
                if (!p.IsDefault) continue;
                if (!found)
                {
                    found = true;
                }
                else
                {
                    p.IsDefault = false;
                }
            }
        }

        private void EnsureOneDefaultAddress()
        {
            if (Addresses.Count == 0) return;

            if (!Addresses.Any(x => x.IsDefault))
                Addresses[0].IsDefault = true;

            var found = false;
            foreach (var a in Addresses)
            {
                if (!a.IsDefault) continue;
                if (!found)
                {
                    found = true;
                }
                else
                {
                    a.IsDefault = false;
                }
            }
        }
    }
}