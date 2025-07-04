using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views;

public partial class ProductListWindow : Window
{
    private List<SanPhamDto> _allProducts = new();
    private readonly ErrorHandler _errorHandler = new WpfErrorHandler();

    public ProductListWindow()
    {
        InitializeComponent();
        _ = LoadDataAsync();
        this.PreviewKeyDown += ProductListWindow_PreviewKeyDown;
    }

    private void ProductListWindow_PreviewKeyDown(object sender, KeyEventArgs e)
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

    private async Task LoadDataAsync()
    {
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            var response = await ApiClient.GetAsync("/api/sanpham");
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<SanPhamDto>>();
                if (data != null)
                {
                    _allProducts = data.OrderBy(x => x.NgungBan).ThenBy(x => x.Ten).ToList();
                    foreach (var p in _allProducts)
                        p.TenNormalized = TextSearchHelper.NormalizeText(p.Ten);

                    ApplySearch();
                }
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                _errorHandler.Handle(new Exception($"API lỗi {(int)response.StatusCode}: {msg}"), "Tải sản phẩm");
            }
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, "LoadDataAsync");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void ApplySearch()
    {
        var keyword = SearchTextBox.Text.Trim();
        var filtered = TextSearchHelper.FilterProducts(_allProducts, keyword);

        for (int i = 0; i < filtered.Count; i++)
            filtered[i].STT = i + 1;

        ProductDataGrid.ItemsSource = filtered;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplySearch();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadDataAsync();
    }

    private async Task OpenEditWindowAsync(SanPhamDto? product = null)
    {
        var window = new ProductEditWindow(product)
        {
            Width = this.ActualWidth,
            Height = this.ActualHeight
        };

        if (window.ShowDialog() == true)
        {
            await LoadDataAsync();
        }
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        await OpenEditWindowAsync();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductDataGrid.SelectedItem is not SanPhamDto selected)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // lấy mới nhất
        var response = await ApiClient.GetAsync($"/api/sanpham/{selected.Id}");
        if (!response.IsSuccessStatusCode) return;

        var latest = await response.Content.ReadFromJsonAsync<SanPhamDto>();
        if (latest != null)
        {
            await OpenEditWindowAsync(latest);
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (ProductDataGrid.SelectedItem is not SanPhamDto selected)
        {
            MessageBox.Show("Vui lòng chọn sản phẩm cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Xoá sản phẩm '{selected.Ten}'?",
            "Xác nhận xoá",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var response = await ApiClient.DeleteAsync($"/api/sanpham/{selected.Id}");

            if (response.IsSuccessStatusCode)
            {
                await LoadDataAsync();
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                _errorHandler.Handle(new Exception($"API lỗi {(int)response.StatusCode}: {msg}"), "Xoá sản phẩm");
            }
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, "DeleteButton_Click");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async void ProductDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var row = ItemsControl.ContainerFromElement(ProductDataGrid, e.OriginalSource as DependencyObject) as DataGridRow;
        if (row?.Item is SanPhamDto selected)
        {
            await OpenEditWindowAsync(selected);
        }
    }
}