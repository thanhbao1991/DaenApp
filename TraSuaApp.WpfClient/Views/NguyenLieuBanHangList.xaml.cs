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

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class NguyenLieuBanHangList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["NguyenLieuBanHang"];

        private Guid? _editingId;

        public NguyenLieuBanHangList()
        {
            InitializeComponent();

            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            PreviewKeyDown += NguyenLieuBanHangList_PreviewKeyDown;
            Closed += NguyenLieuBanHangList_Closed;

            _viewSource.Filter += ViewSource_Filter;
            BindSource();

            AppProviders.NguyenLieuBanHangs.OnChanged += AppProviders_NguyenLieuBanHangs_OnChanged;

            Loaded += async (_, __) =>
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    await AppProviders.NguyenLieuBanHangs.ReloadAsync();
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

        private void NguyenLieuBanHangList_Closed(object? sender, EventArgs e)
        {
            AppProviders.NguyenLieuBanHangs.OnChanged -= AppProviders_NguyenLieuBanHangs_OnChanged;
        }

        private void AppProviders_NguyenLieuBanHangs_OnChanged()
        {
            BindSource();
            ApplySearch();
        }

        private void BindSource()
        {
            _viewSource.Source = AppProviders.NguyenLieuBanHangs.Items;
            NguyenLieuBanHangDataGrid.ItemsSource = _viewSource.View;
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
                new SortDescription(nameof(NguyenLieuBanHangDto.TonKho), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<NguyenLieuBanHangDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not NguyenLieuBanHangDto item)
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
            FormTitleTextBlock.Text = "Thêm nguyên liệu bán hàng";
            SaveButton.Content = "Thêm";

            TenTextBox.Text = string.Empty;
            DonViTinhTextBox.Text = string.Empty;
            TonKhoNumeric.Value = 0;
            DangSuDungCheckBox.IsChecked = true;
            ErrorTextBlock.Text = string.Empty;

            NguyenLieuBanHangDataGrid.UnselectAll();
            TenTextBox.Focus();
        }

        private void SetEditMode(NguyenLieuBanHangDto selected)
        {
            _editingId = selected.Id;
            FormTitleTextBlock.Text = "Sửa nguyên liệu bán hàng";
            SaveButton.Content = "Cập nhật";

            TenTextBox.Text = selected.Ten;
            DonViTinhTextBox.Text = selected.DonViTinh ?? string.Empty;
            TonKhoNumeric.Value = selected.TonKho;
            DangSuDungCheckBox.IsChecked = selected.DangSuDung;
            ErrorTextBlock.Text = string.Empty;

            TenTextBox.Focus();
            TenTextBox.SelectAll();
        }

        private int FindIndex(Guid id)
        {
            for (int i = 0; i < AppProviders.NguyenLieuBanHangs.Items.Count; i++)
            {
                if (AppProviders.NguyenLieuBanHangs.Items[i].Id == id)
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
            if (NguyenLieuBanHangDataGrid.SelectedItem is not NguyenLieuBanHangDto selected)
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

            if (TonKhoNumeric.Value < 0)
            {
                ErrorTextBlock.Text = "Tồn kho không được âm.";
                TonKhoNumeric.Focus();
                return;
            }

            var dto = new NguyenLieuBanHangDto
            {
                Ten = ten,
                DonViTinh = string.IsNullOrWhiteSpace(DonViTinhTextBox.Text) ? null : DonViTinhTextBox.Text.Trim(),
                TonKho = TonKhoNumeric.Value,
                DangSuDung = DangSuDungCheckBox.IsChecked == true
            };

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                HttpResponseMessage response;

                if (_editingId == null)
                {
                    response = await ApiClient.PostAsync("/api/NguyenLieuBanHang", dto);
                }
                else
                {
                    response = await ApiClient.PutAsync($"/api/NguyenLieuBanHang/{_editingId.Value}", dto);
                }

                if (!response.IsSuccessStatusCode)
                {
                    ErrorTextBlock.Text = $"{(_editingId == null ? "Thêm" : "Cập nhật")} thất bại ({(int)response.StatusCode}).";
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<NguyenLieuBanHangDto>>();

                if (result?.IsSuccess == true)
                {
                    if (result.Data != null)
                    {
                        var index = FindIndex(result.Data.Id);

                        if (index >= 0)
                            AppProviders.NguyenLieuBanHangs.Items[index] = result.Data;
                        else
                            AppProviders.NguyenLieuBanHangs.Items.Add(result.Data);
                    }
                    else
                    {
                        await AppProviders.NguyenLieuBanHangs.ReloadAsync();
                    }

                    NotiHelper.ShowSuccess(result.Message);
                    BindSource();
                    ApplySearch();
                    SetAddMode();
                }
                else
                {
                    await AppProviders.NguyenLieuBanHangs.ReloadAsync();
                    BindSource();
                    ApplySearch();
                    SetAddMode();
                    NotiHelper.ShowSuccess(_editingId == null ? "Thêm thành công." : "Cập nhật thành công.");
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
            if (NguyenLieuBanHangDataGrid.SelectedItem is not NguyenLieuBanHangDto selected)
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

                var response = await ApiClient.DeleteAsync($"/api/NguyenLieuBanHang/{selected.Id}");

                if (!response.IsSuccessStatusCode)
                {
                    NotiHelper.ShowError($"Xoá thất bại ({(int)response.StatusCode}).");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<NguyenLieuBanHangDto>>();

                var id = result?.Data?.Id != Guid.Empty
                    ? result!.Data!.Id
                    : selected.Id;

                var index = FindIndex(id);

                if (index >= 0)
                    AppProviders.NguyenLieuBanHangs.Items.RemoveAt(index);
                else
                    await AppProviders.NguyenLieuBanHangs.ReloadAsync();

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

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            SetAddMode();
        }

        private void NguyenLieuBanHangDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (NguyenLieuBanHangDataGrid.SelectedItem is not NguyenLieuBanHangDto selected)
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

        private void NguyenLieuBanHangList_PreviewKeyDown(object sender, KeyEventArgs e)
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