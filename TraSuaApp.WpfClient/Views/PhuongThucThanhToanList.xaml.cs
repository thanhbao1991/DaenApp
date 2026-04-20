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
    public partial class PhuongThucThanhToanList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["PhuongThucThanhToan"];

        private Guid? _editingId;

        public PhuongThucThanhToanList()
        {
            InitializeComponent();

            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            PreviewKeyDown += PhuongThucThanhToanList_PreviewKeyDown;
            Closed += PhuongThucThanhToanList_Closed;

            _viewSource.Filter += ViewSource_Filter;
            BindSource();

            AppProviders.PhuongThucThanhToans.OnChanged += AppProviders_PhuongThucThanhToans_OnChanged;

            Loaded += async (_, __) =>
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    await AppProviders.PhuongThucThanhToans.ReloadAsync();
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

        private void PhuongThucThanhToanList_Closed(object? sender, EventArgs e)
        {
            AppProviders.PhuongThucThanhToans.OnChanged -= AppProviders_PhuongThucThanhToans_OnChanged;
        }

        private void AppProviders_PhuongThucThanhToans_OnChanged()
        {
            BindSource();
            ApplySearch();
        }

        private void BindSource()
        {
            _viewSource.Source = AppProviders.PhuongThucThanhToans.Items;
            PhuongThucThanhToanDataGrid.ItemsSource = _viewSource.View;
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
                new SortDescription(nameof(PhuongThucThanhToanDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<PhuongThucThanhToanDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not PhuongThucThanhToanDto item)
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
            FormTitleTextBlock.Text = "Thêm phương thức thanh toán";
            SaveButton.Content = "Thêm";
            TenTextBox.Text = string.Empty;
            DangSuDungCheckBox.IsChecked = true;
            ErrorTextBlock.Text = string.Empty;
            PhuongThucThanhToanDataGrid.UnselectAll();
            TenTextBox.Focus();
        }

        private void SetEditMode(PhuongThucThanhToanDto selected)
        {
            _editingId = selected.Id;
            FormTitleTextBlock.Text = "Sửa phương thức thanh toán";
            SaveButton.Content = "Cập nhật";
            TenTextBox.Text = selected.Ten;
            DangSuDungCheckBox.IsChecked = selected.DangSuDung;
            ErrorTextBlock.Text = string.Empty;
            TenTextBox.Focus();
            TenTextBox.SelectAll();
        }

        private int FindIndex(Guid id)
        {
            for (int i = 0; i < AppProviders.PhuongThucThanhToans.Items.Count; i++)
            {
                if (AppProviders.PhuongThucThanhToans.Items[i].Id == id)
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
                await AppProviders.PhuongThucThanhToans.ReloadAsync();
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
            if (PhuongThucThanhToanDataGrid.SelectedItem is not PhuongThucThanhToanDto selected)
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

            var dto = new PhuongThucThanhToanDto
            {
                Ten = ten,
                DangSuDung = DangSuDungCheckBox.IsChecked == true,
                
            };

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                HttpResponseMessage response;

                if (_editingId == null)
                {
                    response = await ApiClient.PostAsync("/api/PhuongThucThanhToan", dto);
                }
                else
                {
                    response = await ApiClient.PutAsync($"/api/PhuongThucThanhToan/{_editingId.Value}", dto);
                }

                if (!response.IsSuccessStatusCode)
                {
                    ErrorTextBlock.Text = $"{(_editingId == null ? "Thêm" : "Cập nhật")} thất bại ({(int)response.StatusCode}).";
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<PhuongThucThanhToanDto>>();

                if (result?.IsSuccess == true)
                {
                    if (result.Data != null)
                    {
                        var index = FindIndex(result.Data.Id);

                        if (index >= 0)
                            AppProviders.PhuongThucThanhToans.Items[index] = result.Data;
                        else
                            AppProviders.PhuongThucThanhToans.Items.Add(result.Data);
                    }
                    else
                    {
                        await AppProviders.PhuongThucThanhToans.ReloadAsync();
                    }

                    NotiHelper.ShowSuccess(result.Message);
                    BindSource();
                    ApplySearch();
                    SetAddMode();
                }
                else
                {
                    await AppProviders.PhuongThucThanhToans.ReloadAsync();
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
            if (PhuongThucThanhToanDataGrid.SelectedItem is not PhuongThucThanhToanDto selected)
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

                var response = await ApiClient.DeleteAsync($"/api/PhuongThucThanhToan/{selected.Id}");

                if (!response.IsSuccessStatusCode)
                {
                    NotiHelper.ShowError($"Xoá thất bại ({(int)response.StatusCode}).");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<PhuongThucThanhToanDto>>();

                var id = result?.Data?.Id != Guid.Empty
                    ? result!.Data!.Id
                    : selected.Id;

                var index = FindIndex(id);

                if (index >= 0)
                    AppProviders.PhuongThucThanhToans.Items.RemoveAt(index);
                else
                    await AppProviders.PhuongThucThanhToans.ReloadAsync();

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

        private void PhuongThucThanhToanDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PhuongThucThanhToanDataGrid.SelectedItem is not PhuongThucThanhToanDto selected)
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

        private void PhuongThucThanhToanList_PreviewKeyDown(object sender, KeyEventArgs e)
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