using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views;

public partial class NhomSanPhamListWindow : Window
{
    private List<NhomSanPhamDto> _all = new();
    private readonly WpfErrorHandler _errorHandler = new();

    public NhomSanPhamListWindow()
    {
        InitializeComponent();
        _ = LoadAsync();
        this.PreviewKeyDown += NhomSanPhamListWindow_PreviewKeyDown;
    }

    private async Task OpenEditWindowAsync(NhomSanPhamDto? dto = null)
    {
        var window = new NhomSanPhamEditWindow(dto)
        {
            Width = this.ActualWidth,
            Height = this.ActualHeight
        };
        if (window.ShowDialog() == true)
            await LoadAsync();
    }

    private void NhomSanPhamListWindow_PreviewKeyDown(object sender, KeyEventArgs e)
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
            var response = await ApiClient.GetAsync("/api/nhomsanpham");
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                _errorHandler.Handle(new Exception(msg), "Tải nhóm sản phẩm");
                return;
            }

            var data = await response.Content.ReadFromJsonAsync<List<NhomSanPhamDto>>();
            if (data != null)
            {
                _all = data.OrderBy(x => x.Ten).ToList();
                foreach (var x in _all)
                    x.TenNormalized = TextSearchHelper.NormalizeText(x.Ten ?? "");
                ApplySearch();
            }
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, "LoadAsync");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void ApplySearch()
    {
        var keyword = SearchTextBox.Text.Trim();
        var filtered = TextSearchHelper.FilterNhomSanPhams(_all, keyword);

        for (int i = 0; i < filtered.Count; i++)
            filtered[i].STT = i + 1;

        NhomSanPhamDataGrid.ItemsSource = filtered;
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
        if (NhomSanPhamDataGrid.SelectedItem is not NhomSanPhamDto selected) return;

        var response = await ApiClient.GetAsync($"/api/nhomsanpham/{selected.Id}");
        if (!response.IsSuccessStatusCode) return;

        var latest = await response.Content.ReadFromJsonAsync<NhomSanPhamDto>();
        if (latest != null)
            await OpenEditWindowAsync(latest);
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (NhomSanPhamDataGrid.SelectedItem is not NhomSanPhamDto selected) return;

        var confirm = MessageBox.Show(
            $"Bạn có chắc chắn muốn xoá nhóm '{selected.Ten}'?",
            "Xác nhận xoá",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var response = await ApiClient.DeleteAsync($"/api/nhomsanpham/{selected.Id}");

            if (response.IsSuccessStatusCode)
            {
                await LoadAsync();
            }
            else
            {
                var msg = await response.Content.ReadAsStringAsync();
                _errorHandler.Handle(new Exception(msg), "Xoá nhóm sản phẩm");
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

    private async void NhomSanPhamDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var row = ItemsControl.ContainerFromElement(NhomSanPhamDataGrid, e.OriginalSource as DependencyObject) as DataGridRow;
        if (row == null) return;

        if (row.Item is NhomSanPhamDto selected)
        {
            var response = await ApiClient.GetAsync($"/api/nhomsanpham/{selected.Id}");
            var latest = await response.Content.ReadFromJsonAsync<NhomSanPhamDto>();
            if (latest != null)
                await OpenEditWindowAsync(latest);
        }
    }
}