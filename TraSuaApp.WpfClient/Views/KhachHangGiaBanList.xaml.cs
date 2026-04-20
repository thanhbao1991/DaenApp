using System.ComponentModel;
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
    public partial class KhachHangGiaBanList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["KhachHangGiaBan"];

        private Guid? _editingId;
        private bool _editingIsDeleted;
        private KhachHangGiaBanDto _editingModel = new();

        public KhachHangGiaBanList()
        {
            InitializeComponent();

            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            PreviewKeyDown += KhachHangGiaBanList_PreviewKeyDown;
            Closed += KhachHangGiaBanList_Closed;

            _viewSource.Filter += ViewSource_Filter;
            BindSource();

            AppProviders.KhachHangGiaBans.OnChanged += AppProviders_KhachHangGiaBans_OnChanged;

            Loaded += async (_, __) =>
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    await AppProviders.KhachHangGiaBans.ReloadAsync();
                    if (AppProviders.KhachHangs != null)
                        await AppProviders.KhachHangs.ReloadAsync();

                    BindSource();
                    ApplySearch();
                    SetNoSelectionMode();
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

        private void KhachHangGiaBanList_Closed(object? sender, EventArgs e)
        {
            AppProviders.KhachHangGiaBans.OnChanged -= AppProviders_KhachHangGiaBans_OnChanged;
        }

        private void AppProviders_KhachHangGiaBans_OnChanged()
        {
            BindSource();
            ApplySearch();
        }

        private void BindSource()
        {
            _viewSource.Source = AppProviders.KhachHangGiaBans.Items;
            KhachHangGiaBanDataGrid.ItemsSource = _viewSource.View;
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
                new SortDescription(nameof(KhachHangGiaBanDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<KhachHangGiaBanDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not KhachHangGiaBanDto item)
            {
                e.Accepted = false;
                return;
            }

            var keyword = StringHelper.MyNormalizeText(SearchTextBox.Text.Trim());
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }

        private void SetNoSelectionMode()
        {
            _editingId = null;
            _editingModel = new KhachHangGiaBanDto();

            FormTitleTextBlock.Text = "Cập nhật giá bán";
            SelectedInfoTextBlock.Text = "Chọn một dòng để sửa giá bán.";
            GiaBanTextBox.Value = 0;
            GiaBanTextBox.IsEnabled = false;
            SaveButton.Content = "Lưu";
            SaveButton.IsEnabled = false;
            ErrorTextBlock.Text = string.Empty;

            KhachHangGiaBanDataGrid.UnselectAll();
        }

        private void SetEditMode(KhachHangGiaBanDto selected)
        {
            _editingId = selected.Id;

            _editingModel = new KhachHangGiaBanDto
            {
                Id = selected.Id,
                KhachHangId = selected.KhachHangId,
                SanPhamBienTheId = selected.SanPhamBienTheId,
                GiaBan = selected.GiaBan,
                LastModified = selected.LastModified
            };

            SelectedInfoTextBlock.Text = $"{selected.TenKhachHang} - {selected.TenSanPham} / {selected.TenBienThe}";
            GiaBanTextBox.Value = selected.GiaBan;
            SaveButton.IsEnabled = true;
            ErrorTextBlock.Text = string.Empty;

            GiaBanTextBox.Focus();
            GiaBanTextBox.SelectAll();
        }

        private int FindIndex(Guid id)
        {
            for (int i = 0; i < AppProviders.KhachHangGiaBans.Items.Count; i++)
            {
                if (AppProviders.KhachHangGiaBans.Items[i].Id == id)
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
                await AppProviders.KhachHangGiaBans.ReloadAsync();
                BindSource();
                ApplySearch();
                SetNoSelectionMode();
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
            MessageBox.Show(
                "Màn này chỉ dùng để sửa giá bán, không hỗ trợ thêm mới.",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (KhachHangGiaBanDataGrid.SelectedItem is not KhachHangGiaBanDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetEditMode(selected);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = string.Empty;

            if (_editingId == null)
            {
                MessageBox.Show("Vui lòng chọn một dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_editingIsDeleted && GiaBanTextBox.Value < 0)
            {
                ErrorTextBlock.Text = "Giá bán không được âm.";
                GiaBanTextBox.Focus();
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                _editingModel.GiaBan = GiaBanTextBox.Value;

                HttpResponseMessage response = await ApiClient.PutAsync(
                    $"/api/KhachHangGiaBan/{_editingId.Value}",
                    _editingModel);

                if (!response.IsSuccessStatusCode)
                {
                    ErrorTextBlock.Text = $"Cập nhật thất bại ({(int)response.StatusCode}).";
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<KhachHangGiaBanDto>>();

                if (result?.IsSuccess == true)
                {
                    if (result.Data != null)
                    {
                        var index = FindIndex(result.Data.Id);

                        if (index >= 0)
                            AppProviders.KhachHangGiaBans.Items[index] = result.Data;
                        else
                            AppProviders.KhachHangGiaBans.Items.Add(result.Data);
                    }
                    else
                    {
                        await AppProviders.KhachHangGiaBans.ReloadAsync();
                    }

                    NotiHelper.ShowSuccess(result.Message);
                    BindSource();
                    ApplySearch();
                    SetNoSelectionMode();
                }
                else
                {
                    await AppProviders.KhachHangGiaBans.ReloadAsync();
                    BindSource();
                    ApplySearch();
                    SetNoSelectionMode();
                    NotiHelper.ShowSuccess("Cập nhật giá bán thành công.");
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
            if (KhachHangGiaBanDataGrid.SelectedItem is not KhachHangGiaBanDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var label = $"{selected.TenKhachHang} - {selected.TenSanPham} / {selected.TenBienThe}";
            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} của '{label}'?",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response = await ApiClient.DeleteAsync($"/api/KhachHangGiaBan/{selected.Id}");

                if (!response.IsSuccessStatusCode)
                {
                    NotiHelper.ShowError($"Xoá thất bại ({(int)response.StatusCode}).");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<KhachHangGiaBanDto>>();

                var id = result?.Data?.Id != Guid.Empty
                    ? result!.Data!.Id
                    : selected.Id;

                var index = FindIndex(id);

                if (index >= 0)
                    AppProviders.KhachHangGiaBans.Items.RemoveAt(index);
                else
                    await AppProviders.KhachHangGiaBans.ReloadAsync();

                NotiHelper.ShowSuccess(result?.Message ?? "Xoá thành công.");
                BindSource();
                ApplySearch();
                SetNoSelectionMode();
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

        private void KhachHangGiaBanDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (KhachHangGiaBanDataGrid.SelectedItem is not KhachHangGiaBanDto selected)
                return;

            SetEditMode(selected);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            SetNoSelectionMode();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void KhachHangGiaBanList_PreviewKeyDown(object sender, KeyEventArgs e)
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