using System.ComponentModel;
using System.Globalization;
using System.Net.Http;
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

namespace TraSuaApp.WpfClient.SettingsViews
{
    public partial class NguyenLieuList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["NguyenLieu"];

        private Guid? _editingId;
        private List<NguyenLieuBanHangDto> _nguyenLieuBanHangList = new();

        public NguyenLieuList()
        {
            InitializeComponent();

            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            PreviewKeyDown += NguyenLieuList_PreviewKeyDown;
            Closed += NguyenLieuList_Closed;

            _viewSource.Filter += ViewSource_Filter;
            BindSource();

            AppProviders.NguyenLieus.OnChanged += AppProviders_NguyenLieus_OnChanged;
            AppProviders.NguyenLieuBanHangs.OnChanged += AppProviders_NguyenLieuBanHangs_OnChanged;

            Loaded += async (_, __) =>
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    await AppProviders.NguyenLieuBanHangs.ReloadAsync();
                    await AppProviders.NguyenLieus.ReloadAsync();

                    LoadLookupList();
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

        private void NguyenLieuList_Closed(object? sender, EventArgs e)
        {
            AppProviders.NguyenLieus.OnChanged -= AppProviders_NguyenLieus_OnChanged;
            AppProviders.NguyenLieuBanHangs.OnChanged -= AppProviders_NguyenLieuBanHangs_OnChanged;
        }

        private void AppProviders_NguyenLieus_OnChanged()
        {
            BindSource();
            ApplySearch();
        }

        private void AppProviders_NguyenLieuBanHangs_OnChanged()
        {
            LoadLookupList();
            ApplySearch();
        }

        private void LoadLookupList()
        {
            _nguyenLieuBanHangList = AppProviders.NguyenLieuBanHangs.Items.ToList();
            NguyenLieuBanHangSearchBox.NguyenLieuBanHangList = _nguyenLieuBanHangList;
        }

        private void BindSource()
        {
            _viewSource.Source = AppProviders.NguyenLieus.Items;
            NguyenLieuDataGrid.ItemsSource = _viewSource.View;
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
                new SortDescription(nameof(NguyenLieuDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<NguyenLieuDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not NguyenLieuDto item)
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
            FormTitleTextBlock.Text = "Thêm nguyên liệu";
            SaveButton.Content = "Thêm";

            TenTextBox.Text = string.Empty;
            DonViTinhTextBox.Text = string.Empty;
            GiaNhapTextBox.Value = 0;
            HeSoQuyDoiNumeric.Value = 0;
            DangSuDungCheckBox.IsChecked = true;
            ErrorTextBlock.Text = string.Empty;

            NguyenLieuBanHangSearchBox.SearchTextBox.Text = string.Empty;
            NguyenLieuDataGrid.UnselectAll();
            TenTextBox.Focus();
        }

        private void SetEditMode(NguyenLieuDto selected)
        {
            _editingId = selected.Id;
            FormTitleTextBlock.Text = "Sửa nguyên liệu";
            SaveButton.Content = "Cập nhật";

            TenTextBox.Text = selected.Ten;
            DonViTinhTextBox.Text = selected.DonViTinh ?? string.Empty;
            GiaNhapTextBox.Value = selected.GiaNhap;
            HeSoQuyDoiNumeric.Value = selected.HeSoQuyDoiBanHang ?? 0;
            DangSuDungCheckBox.IsChecked = selected.DangSuDung;
            ErrorTextBlock.Text = string.Empty;

            if (selected.NguyenLieuBanHangId.HasValue)
                NguyenLieuBanHangSearchBox.SetSelectedNguyenLieuBanHangByIdWithoutPopup(selected.NguyenLieuBanHangId.Value);
            else
                NguyenLieuBanHangSearchBox.SearchTextBox.Text = string.Empty;

            TenTextBox.Focus();
            TenTextBox.SelectAll();
        }

        private int FindIndex(Guid id)
        {
            for (int i = 0; i < AppProviders.NguyenLieus.Items.Count; i++)
            {
                if (AppProviders.NguyenLieus.Items[i].Id == id)
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

                await AppProviders.NguyenLieuBanHangs.ReloadAsync();
                await AppProviders.NguyenLieus.ReloadAsync();

                LoadLookupList();
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
            if (NguyenLieuDataGrid.SelectedItem is not NguyenLieuDto selected)
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

            var giaNhap = GiaNhapTextBox.Value;
            var donViTinh = DonViTinhTextBox.Text.Trim();
            var selectedNguyenLieuBanHang = NguyenLieuBanHangSearchBox.SelectedNguyenLieuBanHang;

            if (selectedNguyenLieuBanHang != null && HeSoQuyDoiNumeric.Value <= 0)
            {
                ErrorTextBlock.Text = "Hệ số quy đổi phải lớn hơn 0.";
                HeSoQuyDoiNumeric.Focus();
                return;
            }

            var dto = new NguyenLieuDto
            {
                Ten = ten,
                GiaNhap = giaNhap,
                DonViTinh = string.IsNullOrWhiteSpace(donViTinh) ? null : donViTinh,
                HeSoQuyDoiBanHang = selectedNguyenLieuBanHang == null ? null : HeSoQuyDoiNumeric.Value,
                NguyenLieuBanHangId = selectedNguyenLieuBanHang?.Id,
                DangSuDung = DangSuDungCheckBox.IsChecked == true
            };

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                HttpResponseMessage response;

                if (_editingId == null)
                {
                    response = await ApiClient.PostAsync("/api/NguyenLieu", dto);
                }
                else
                {
                    response = await ApiClient.PutAsync($"/api/NguyenLieu/{_editingId.Value}", dto);
                }

                if (!response.IsSuccessStatusCode)
                {
                    ErrorTextBlock.Text = $"{(_editingId == null ? "Thêm" : "Cập nhật")} thất bại ({(int)response.StatusCode}).";
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<NguyenLieuDto>>();

                if (result?.IsSuccess == true)
                {
                    if (result.Data != null)
                    {
                        var index = FindIndex(result.Data.Id);

                        if (index >= 0)
                            AppProviders.NguyenLieus.Items[index] = result.Data;
                        else
                            AppProviders.NguyenLieus.Items.Add(result.Data);
                    }
                    else
                    {
                        await AppProviders.NguyenLieus.ReloadAsync();
                    }

                    NotiHelper.ShowSuccess(result.Message);
                    BindSource();
                    ApplySearch();
                    SetAddMode();
                }
                else
                {
                    await AppProviders.NguyenLieus.ReloadAsync();
                    BindSource();
                    ApplySearch();
                    SetAddMode();
                    NotiHelper.ShowSuccess(_editingId == null ? "Thêm nguyên liệu thành công." : "Cập nhật nguyên liệu thành công.");
                }
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
            if (NguyenLieuDataGrid.SelectedItem is not NguyenLieuDto selected)
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

                var response = await ApiClient.DeleteAsync($"/api/NguyenLieu/{selected.Id}");

                if (!response.IsSuccessStatusCode)
                {
                    NotiHelper.ShowError($"Xoá thất bại ({(int)response.StatusCode}).");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<NguyenLieuDto>>();

                var id = result?.Data?.Id != Guid.Empty
                    ? result!.Data!.Id
                    : selected.Id;

                var index = FindIndex(id);

                if (index >= 0)
                    AppProviders.NguyenLieus.Items.RemoveAt(index);
                else
                    await AppProviders.NguyenLieus.ReloadAsync();

                NotiHelper.ShowSuccess(result?.Message ?? "Xoá nguyên liệu thành công.");
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

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            SetAddMode();
        }

        private void NguyenLieuDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (NguyenLieuDataGrid.SelectedItem is not NguyenLieuDto selected)
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

        private void NguyenLieuList_PreviewKeyDown(object sender, KeyEventArgs e)
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
    }

    public class NguyenLieuBanHangNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Guid id || id == Guid.Empty) return string.Empty;

            var item = AppProviders.NguyenLieuBanHangs?.Items.FirstOrDefault(x => x.Id == id);
            return item?.Ten ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class NguyenLieuBanHangDonViConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Guid id || id == Guid.Empty) return string.Empty;

            var item = AppProviders.NguyenLieuBanHangs?.Items.FirstOrDefault(x => x.Id == id);
            return item?.DonViTinh ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}