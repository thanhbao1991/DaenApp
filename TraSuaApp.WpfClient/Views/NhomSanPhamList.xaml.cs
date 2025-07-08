using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views;

public partial class NhomSanPhamList : Window
{
    private List<NhomSanPhamDto> _all = new();
    private readonly WpfErrorHandler _errorHandler = new();

    public NhomSanPhamList()
    {
        InitializeComponent();
        _ = LoadAsync();
        this.PreviewKeyDown += NhomSanPhamListWindow_PreviewKeyDown;
    }
    private async Task LoadAsync()
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var response = await ApiClient.GetAsync("/api/nhomsanpham");
            var result = await response.Content.ReadFromJsonAsync<Result<List<NhomSanPhamDto>>>();

            if (result?.IsSuccess != true || result.Data == null)
            {
                throw new Exception(result?.Message ?? "Không thể tải danh sách nhóm sản phẩm.");
            }

            _all = result.Data.OrderBy(x => x.Ten).ToList();
            foreach (var x in _all)
                x.TenNormalized = TextSearchHelper.NormalizeText(x.Ten ?? "");

            ApplySearch();
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

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (NhomSanPhamDataGrid.SelectedItem is not NhomSanPhamDto selected) return;

        var response = await ApiClient.GetAsync($"/api/nhomsanpham/{selected.Id}");
        var result = await response.Content.ReadFromJsonAsync<Result<NhomSanPhamDto>>();

        if (result?.IsSuccess != true || result.Data == null) return;

        await OpenEditWindowAsync(result.Data);
    }

    private async void NhomSanPhamDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var row = ItemsControl.ContainerFromElement(NhomSanPhamDataGrid, e.OriginalSource as DependencyObject) as DataGridRow;
        if (row?.Item is not NhomSanPhamDto selected) return;

        var response = await ApiClient.GetAsync($"/api/nhomsanpham/{selected.Id}");
        var result = await response.Content.ReadFromJsonAsync<Result<NhomSanPhamDto>>();

        if (result?.IsSuccess != true || result.Data == null) return;

        await OpenEditWindowAsync(result.Data);
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
            var result = await response.Content.ReadFromJsonAsync<Result<bool>>();

            if (result?.IsSuccess == true)
            {
                await LoadAsync();
            }
            else
            {
                throw new Exception(result?.Message ?? "Không thể xoá nhóm sản phẩm.");
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
    private async Task OpenEditWindowAsync(NhomSanPhamDto? dto = null)
    {
        var window = new NhomSanPhamEdit(dto)
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


    private void ApplySearch()
    {
        var keyword = SearchTextBox.Text.Trim();
        var filtered = TextSearchHelper.FilterByTen(_all, keyword, x => x.Ten);

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

}