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

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class TaiKhoanList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["TaiKhoan"];

        private Guid? _editingId;

        public TaiKhoanList()
        {
            InitializeComponent();

            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            PreviewKeyDown += TaiKhoanList_PreviewKeyDown;
            Closed += TaiKhoanList_Closed;

            _viewSource.Filter += ViewSource_Filter;
            BindSource();

            AppProviders.TaiKhoans.OnChanged += AppProviders_TaiKhoans_OnChanged;

            Loaded += async (_, __) =>
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    await AppProviders.TaiKhoans.ReloadAsync();
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

        private void TaiKhoanList_Closed(object? sender, EventArgs e)
        {
            AppProviders.TaiKhoans.OnChanged -= AppProviders_TaiKhoans_OnChanged;
        }

        private void AppProviders_TaiKhoans_OnChanged()
        {
            BindSource();
            ApplySearch();
        }

        private void BindSource()
        {
            _viewSource.Source = AppProviders.TaiKhoans.Items;
            TaiKhoanDataGrid.ItemsSource = _viewSource.View;
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
                new SortDescription(nameof(TaiKhoanDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<TaiKhoanDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not TaiKhoanDto item)
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
            FormTitleTextBlock.Text = "Thêm tài khoản";
            SaveButton.Content = "Thêm";

            TenDangNhapTextBox.IsEnabled = true;
            TenDangNhapTextBox.Text = string.Empty;
            MatKhauBox.Clear();
            TenHienThiTextBox.Text = string.Empty;
            VaiTroComboBox.SelectedIndex = 1;
            IsActiveCheckBox.IsChecked = true;
            ErrorTextBlock.Text = string.Empty;

            TaiKhoanDataGrid.UnselectAll();
            TenDangNhapTextBox.Focus();
        }

        private void SetEditMode(TaiKhoanDto selected)
        {
            _editingId = selected.Id;



            TenDangNhapTextBox.Text = selected.TenDangNhap;
            MatKhauBox.Clear();
            TenHienThiTextBox.Text = selected.TenHienThi;

            foreach (ComboBoxItem item in VaiTroComboBox.Items)
            {
                if ((string?)item.Content == selected.VaiTro)
                {
                    VaiTroComboBox.SelectedItem = item;
                    break;
                }
            }

            IsActiveCheckBox.IsChecked = selected.IsActive;
            ErrorTextBlock.Text = string.Empty;
            TenDangNhapTextBox.Focus();
            TenDangNhapTextBox.SelectAll();
        }

        private int FindIndex(Guid id)
        {
            for (int i = 0; i < AppProviders.TaiKhoans.Items.Count; i++)
            {
                if (AppProviders.TaiKhoans.Items[i].Id == id)
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
                await AppProviders.TaiKhoans.ReloadAsync();
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
            if (TaiKhoanDataGrid.SelectedItem is not TaiKhoanDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetEditMode(selected);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = string.Empty;

            var tenDangNhap = TenDangNhapTextBox.Text.Trim();
            var tenHienThi = TenHienThiTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(tenDangNhap))
            {
                ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                TenDangNhapTextBox.Focus();
                return;
            }

            if (VaiTroComboBox.SelectedItem is not ComboBoxItem vaiTroItem || string.IsNullOrWhiteSpace(vaiTroItem.Content?.ToString()))
            {
                ErrorTextBlock.Text = "Vui lòng chọn vai trò.";
                VaiTroComboBox.Focus();
                return;
            }

            var dto = new TaiKhoanDto
            {
                TenDangNhap = tenDangNhap,
                TenHienThi = tenHienThi,
                VaiTro = vaiTroItem.Content?.ToString() ?? "Nhân viên",
                IsActive = IsActiveCheckBox.IsChecked == true,

            };

            var matKhau = MatKhauBox.Password.Trim();
            if (!string.IsNullOrWhiteSpace(matKhau))
                dto.MatKhau = matKhau;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var api = Apis.TaiKhoan;
                Result<TaiKhoanDto> result;

                if (_editingId == null)
                {
                    result = await api.CreateAsync(dto);
                }
                else
                {
                    result = await api.UpdateAsync(_editingId.Value, dto);
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
                        AppProviders.TaiKhoans.Items[index] = result.Data;
                    else
                        AppProviders.TaiKhoans.Items.Add(result.Data);
                }
                else
                {
                    await AppProviders.TaiKhoans.ReloadAsync();
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
            if (TaiKhoanDataGrid.SelectedItem is not TaiKhoanDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} '{selected.TenDangNhap}'?",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response = await ApiClient.DeleteAsync($"/api/TaiKhoan/{selected.Id}");

                if (!response.IsSuccessStatusCode)
                {
                    NotiHelper.ShowError($"Xoá thất bại ({(int)response.StatusCode}).");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<TaiKhoanDto>>();

                var id = result?.Data?.Id != Guid.Empty
                    ? result!.Data!.Id
                    : selected.Id;

                var index = FindIndex(id);

                if (index >= 0)
                    AppProviders.TaiKhoans.Items.RemoveAt(index);
                else
                    await AppProviders.TaiKhoans.ReloadAsync();

                NotiHelper.ShowSuccess(result?.Message ?? "Xoá tài khoản thành công.");
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

        private void TaiKhoanDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TaiKhoanDataGrid.SelectedItem is not TaiKhoanDto selected)
                return;

            SetAddMode();
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

        private void TaiKhoanList_PreviewKeyDown(object sender, KeyEventArgs e)
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
}