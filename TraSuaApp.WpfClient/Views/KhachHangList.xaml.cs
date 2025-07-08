using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views;

public partial class KhachHangList : Window
{
    private List<KhachHangDto> _allCustomers = new();
    private readonly ErrorHandler _errorHandler = new WpfErrorHandler();

    public KhachHangList()
    {
        InitializeComponent();
        _ = LoadDataAsync();
        this.PreviewKeyDown += KhachHangList_PreviewKeyDown;
    }

    private void KhachHangList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            _ = OpenEditWindowAsync();
            e.Handled = true;
        }
        else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
        {
            _ = OpenEditWindowAsync(GetSelectedCustomer());
            e.Handled = true;
        }
        else if (e.Key == Key.Delete)
        {
            _ = DeleteSelectedCustomer();
            e.Handled = true;
        }
        else if (e.Key == Key.F5)
        {
            _ = LoadDataAsync();
            e.Handled = true;
        }
    }

    private async Task LoadDataAsync()
    {
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            var response = await ApiClient.GetAsync("/api/khachhang");
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Result<List<KhachHangDto>>>();
                if (result?.IsSuccess == true && result.Data != null)
                {
                    _allCustomers = result.Data.OrderBy(x => x.Ten).ToList();

                    for (int i = 0; i < _allCustomers.Count; i++)
                    {
                        var kh = _allCustomers[i];
                        kh.STT = i + 1;
                        kh.TenNormalized = TextSearchHelper.NormalizeText(kh.Ten);
                    }

                    ApplySearch();
                }
                else
                {
                    throw new Exception(result?.Message ?? "Không tải được danh sách khách hàng.");
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
            _errorHandler.Handle(ex, "Tải danh sách khách hàng");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void ApplySearch()
    {
        var keyword = SearchTextBox.Text.Trim().ToLower();

        var filtered = _allCustomers.Where(x =>
            (x.Ten ?? "").ToLower().Contains(keyword)
            || (x.Phones.FirstOrDefault(p => p.IsDefault)?.SoDienThoai ?? "").ToLower().Contains(keyword)
            || (x.Addresses.FirstOrDefault(a => a.IsDefault)?.DiaChi ?? "").ToLower().Contains(keyword)
        ).ToList();

        for (int i = 0; i < filtered.Count; i++)
            filtered[i].STT = i + 1;

        CustomerDataGrid.ItemsSource = filtered;
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

    private async Task OpenEditWindowAsync(KhachHangDto? customer = null)
    {
        var window = new KhachHangEdit(customer)
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
        var selected = GetSelectedCustomer();
        if (selected != null)
        {
            try
            {
                var response = await ApiClient.GetAsync($"/api/khachhang/{selected.Id}");
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<Result<KhachHangDto>>();
                    if (result?.IsSuccess == true && result.Data != null)
                    {
                        await OpenEditWindowAsync(result.Data);
                    }
                    else
                    {
                        throw new Exception(result?.Message ?? "Không đọc được thông tin khách hàng.");
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
                _errorHandler.Handle(ex, "Tải khách hàng");
            }
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        await DeleteSelectedCustomer();
    }

    private async Task DeleteSelectedCustomer()
    {
        var selected = GetSelectedCustomer();
        if (selected == null)
        {
            MessageBox.Show("Vui lòng chọn khách hàng cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"Xoá khách hàng '{selected.Ten}'?",
            "Xác nhận xoá",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            var response = await ApiClient.DeleteAsync($"/api/khachhang/{selected.Id}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Result<object>>();
                if (result?.IsSuccess == true)
                {
                    await LoadDataAsync();
                }
                else
                {
                    throw new Exception(result?.Message ?? "Xoá khách hàng thất bại.");
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
            _errorHandler.Handle(ex, "Xoá khách hàng");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async void CustomerDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CustomerDataGrid.SelectedItem is KhachHangDto selected)
        {
            await OpenEditWindowAsync(selected);
        }
    }

    private KhachHangDto? GetSelectedCustomer()
    {
        return CustomerDataGrid.SelectedItem as KhachHangDto;
    }
}