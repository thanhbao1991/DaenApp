﻿using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views;

public partial class SanPhamList : Window
{
    private List<SanPhamDto> _allProducts = new();
    private readonly UIExceptionHelper _errorHandler = new WpfErrorHandler();

    public SanPhamList()
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
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                _errorHandler.Handle(new Exception($"{msg}"), "Tải sản phẩm");
                return;
            }

            var result = await response.Content.ReadFromJsonAsync<Result<List<SanPhamDto>>>();
            if (result?.IsSuccess != true)
            {
                throw new Exception(result?.Message ?? "Không thể tải sản phẩm.");
            }

            _allProducts = result.Data
                .OrderBy(x => x.NgungBan)
                .ThenBy(x => x.Ten)
                .ToList();


            ApplySearch();
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
        var filtered = TextSearchHelper.FilterSanPhams(_allProducts, keyword);

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
        var window = new SanPhamEdit(product)
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

        var response = await ApiClient.GetAsync($"/api/sanpham/{selected.Id}");
        if (!response.IsSuccessStatusCode) return;

        var result = await response.Content.ReadFromJsonAsync<Result<SanPhamDto>>();
        if (result?.IsSuccess == true && result.Data != null)
        {
            await OpenEditWindowAsync(result.Data);
        }
        else
        {
            MessageBox.Show(result?.Message ?? "Không thể lấy dữ liệu sản phẩm.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                _errorHandler.Handle(new Exception($"{msg}"), "Xoá sản phẩm");
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
            var response = await ApiClient.GetAsync($"/api/sanpham/{selected.Id}");
            var result = await response.Content.ReadFromJsonAsync<Result<SanPhamDto>>();
            if (result?.IsSuccess == true && result.Data != null)
                await OpenEditWindowAsync(result.Data);
        }
    }
}