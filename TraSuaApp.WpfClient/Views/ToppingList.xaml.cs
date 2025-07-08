using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views;

public partial class ToppingList : Window
{
    private List<ToppingDto> _all = new();
    private readonly WpfErrorHandler _errorHandler = new();

    public ToppingList()
    {
        InitializeComponent();
        _ = LoadAsync();
        this.PreviewKeyDown += ToppingListWindow_PreviewKeyDown;
    }

    private async Task OpenEditWindowAsync(ToppingDto? dto = null)
    {
        var window = new ToppingEdit(dto)
        {
            Width = this.ActualWidth,
            Height = this.ActualHeight
        };
        if (window.ShowDialog() == true)
            await LoadAsync();
    }

    private void ToppingListWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            AddButton_Click(null!, null!); e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
        {
            EditButton_Click(null!, null!); e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            DeleteButton_Click(null!, null!); e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            ReloadButton_Click(null!, null!); e.Handled = true;
        }
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var response = await ApiClient.GetAsync("/api/topping");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Result<List<ToppingDto>>>();
                if (result?.IsSuccess == true && result.Data != null)
                {
                    _all = result.Data.OrderBy(x => x.Ten).ToList();
                    foreach (var x in _all)
                        x.TenNormalized = TextSearchHelper.NormalizeText(x.Ten ?? "");
                    ApplySearch();
                }
                else
                {
                    throw new Exception(result?.Message ?? "Không thể tải danh sách topping.");
                }
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new Exception($"API lỗi {(int)response.StatusCode}: {msg}");
            }
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, "Load topping");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void ApplySearch()
    {
        var keyword = SearchTextBox.Text.Trim();
        var filtered = TextSearchHelper.FilterByTen(_all, keyword, x => x.Ten);

        for (int i = 0; i < filtered.Count; i++)
            filtered[i].STT = i + 1;

        ToppingDataGrid.ItemsSource = filtered;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplySearch();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        await OpenEditWindowAsync();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (ToppingDataGrid.SelectedItem is not ToppingDto selected) return;

        try
        {
            var response = await ApiClient.GetAsync($"/api/topping/{selected.Id}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Result<ToppingDto>>();
                if (result?.IsSuccess == true && result.Data != null)
                {
                    await OpenEditWindowAsync(result.Data);
                }
                else
                {
                    throw new Exception(result?.Message ?? "Không đọc được topping.");
                }
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new Exception($"API lỗi {(int)response.StatusCode}: {msg}");
            }
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, "Xem topping");
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (ToppingDataGrid.SelectedItem is not ToppingDto selected) return;

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xoá topping '{selected.Ten}'?",
            "Xác nhận xoá",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var response = await ApiClient.DeleteAsync($"/api/topping/{selected.Id}");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Result<object>>();
                if (result?.IsSuccess == true)
                {
                    await LoadAsync();
                }
                else
                {
                    throw new Exception(result?.Message ?? "Xoá topping thất bại.");
                }
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new Exception($"API lỗi {(int)response.StatusCode}: {msg}");
            }
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, "Xoá topping");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async void ToppingDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var row = ItemsControl.ContainerFromElement(ToppingDataGrid, e.OriginalSource as DependencyObject) as DataGridRow;
        if (row == null) return;

        if (row.Item is ToppingDto selected)
        {
            try
            {
                var response = await ApiClient.GetAsync($"/api/topping/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<ToppingDto>>();
                if (result?.IsSuccess == true && result.Data != null)
                {
                    await OpenEditWindowAsync(result.Data);
                }
                else
                {
                    throw new Exception(result?.Message ?? "Không đọc được topping.");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Xem topping");
            }
        }
    }
}