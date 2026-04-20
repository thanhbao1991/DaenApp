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
using TraSuaApp.WpfClient.Models;

namespace TraSuaApp.WpfClient.SettingsViews
{
    public partial class ToppingList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["Topping"];

        private Guid? _editingId;
        private ToppingDto _editingModel = new();
        private List<NhomSanPhamCheckItem> _bindingList = new();

        public ToppingList()
        {
            InitializeComponent();

            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            PreviewKeyDown += ToppingList_PreviewKeyDown;
            Closed += ToppingList_Closed;

            _viewSource.Filter += ViewSource_Filter;
            BindSource();

            AppProviders.Toppings.OnChanged += AppProviders_Toppings_OnChanged;
            AppProviders.NhomSanPhams.OnChanged += AppProviders_NhomSanPhams_OnChanged;

            Loaded += async (_, __) =>
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    if (AppProviders.NhomSanPhams.Items.Count == 0)
                        await AppProviders.NhomSanPhams.ReloadAsync();

                    await AppProviders.Toppings.ReloadAsync();

                    RefreshGroupList();
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

        private void ToppingList_Closed(object? sender, EventArgs e)
        {
            AppProviders.Toppings.OnChanged -= AppProviders_Toppings_OnChanged;
            AppProviders.NhomSanPhams.OnChanged -= AppProviders_NhomSanPhams_OnChanged;
        }

        private void AppProviders_Toppings_OnChanged()
        {
            BindSource();
            ApplySearch();
        }

        private void AppProviders_NhomSanPhams_OnChanged()
        {
            RefreshGroupList();
        }

        private void BindSource()
        {
            _viewSource.Source = AppProviders.Toppings.Items;
            ToppingDataGrid.ItemsSource = _viewSource.View;
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
                new SortDescription(nameof(ToppingDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<ToppingDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not ToppingDto item)
            {
                e.Accepted = false;
                return;
            }

            var keyword = StringHelper.MyNormalizeText(SearchTextBox.Text.Trim());
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }

        private void RefreshGroupList(IEnumerable<Guid>? selectedIds = null)
        {
            var ids = selectedIds?.ToHashSet() ?? new HashSet<Guid>();

            _bindingList = AppProviders.NhomSanPhams.Items
                .Select(g => new NhomSanPhamCheckItem
                {
                    Id = g.Id,
                    Ten = g.Ten,
                    IsChecked = ids.Contains(g.Id)
                })
                .ToList();

            NhomSanPhamListBox.ItemsSource = _bindingList;
        }

        private void SetAddMode()
        {
            _editingId = null;
            _editingModel = new ToppingDto
            {
                NgungBan = true
            };

            FormTitleTextBlock.Text = "Thêm topping";
            SaveButton.Content = "Thêm";

            TenTextBox.Text = string.Empty;
            GiaTextBox.Value = 0;
            KichHoatCheckBox.IsChecked = true;

            RefreshGroupList();
            ToppingDataGrid.UnselectAll();
            TenTextBox.Focus();
        }

        private void SetEditMode(ToppingDto selected)
        {
            _editingId = selected.Id;
            _editingModel = new ToppingDto
            {
                Id = selected.Id,
                Ten = selected.Ten,
                Gia = selected.Gia,
                NgungBan = selected.NgungBan,

                LastModified = selected.LastModified,
                NhomSanPhams = selected.NhomSanPhams?.ToList() ?? new List<Guid>()
            };

            FormTitleTextBlock.Text = "Sửa topping";
            SaveButton.Content = "Cập nhật";

            TenTextBox.Text = _editingModel.Ten;
            GiaTextBox.Value = _editingModel.Gia;
            KichHoatCheckBox.IsChecked = _editingModel.NgungBan;

            RefreshGroupList(_editingModel.NhomSanPhams);
            TenTextBox.Focus();
            TenTextBox.SelectAll();
        }

        private int FindIndex(Guid id)
        {
            for (int i = 0; i < AppProviders.Toppings.Items.Count; i++)
            {
                if (AppProviders.Toppings.Items[i].Id == id)
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
                await AppProviders.Toppings.ReloadAsync();
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
            if (ToppingDataGrid.SelectedItem is not ToppingDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetEditMode(selected);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var ten = TenTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(ten))
            {
                MessageBox.Show($"Tên {_friendlyName} không được để trống.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                TenTextBox.Focus();
                return;
            }

            _editingModel.Ten = ten;
            _editingModel.Gia = GiaTextBox.Value;
            _editingModel.NgungBan = KichHoatCheckBox.IsChecked == true;
            _editingModel.NhomSanPhams = _bindingList
                .Where(x => x.IsChecked)
                .Select(x => x.Id)
                .ToList();

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                HttpResponseMessage response;

                if (_editingId == null)
                {
                    response = await ApiClient.PostAsync("/api/Topping", _editingModel);
                }
                else
                {
                    response = await ApiClient.PutAsync($"/api/Topping/{_editingId.Value}", _editingModel);
                }

                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show(
                        $"{(_editingId == null ? "Thêm" : "Cập nhật")} thất bại ({(int)response.StatusCode}).",
                        "Thông báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<ToppingDto>>();

                if (result?.IsSuccess == true)
                {
                    if (result.Data != null)
                    {
                        var index = FindIndex(result.Data.Id);

                        if (index >= 0)
                            AppProviders.Toppings.Items[index] = result.Data;
                        else
                            AppProviders.Toppings.Items.Add(result.Data);
                    }
                    else
                    {
                        await AppProviders.Toppings.ReloadAsync();
                    }

                    NotiHelper.ShowSuccess(result.Message);
                    BindSource();
                    ApplySearch();
                    SetAddMode();
                }
                else
                {
                    await AppProviders.Toppings.ReloadAsync();
                    BindSource();
                    ApplySearch();
                    SetAddMode();
                    NotiHelper.ShowSuccess(_editingId == null ? "Thêm topping thành công." : "Cập nhật topping thành công.");
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
            if (ToppingDataGrid.SelectedItem is not ToppingDto selected)
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

                var response = await ApiClient.DeleteAsync($"/api/Topping/{selected.Id}");

                if (!response.IsSuccessStatusCode)
                {
                    NotiHelper.ShowError($"Xoá thất bại ({(int)response.StatusCode}).");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<ToppingDto>>();

                var id = result?.Data?.Id != Guid.Empty
                    ? result!.Data!.Id
                    : selected.Id;

                var index = FindIndex(id);

                if (index >= 0)
                    AppProviders.Toppings.Items.RemoveAt(index);
                else
                    await AppProviders.Toppings.ReloadAsync();

                NotiHelper.ShowSuccess(result?.Message ?? "Xoá topping thành công.");
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

        private void ToppingDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ToppingDataGrid.SelectedItem is not ToppingDto selected)
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

        private void ToppingList_PreviewKeyDown(object sender, KeyEventArgs e)
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