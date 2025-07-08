using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views;

public partial class ToppingEdit : Window
{
    public ToppingDto Data { get; private set; }
    private readonly bool _isEdit;
    private readonly WpfErrorHandler _errorHandler;
    private List<NhomSanPhamCheckItem> _bindingList = new();

    public ToppingEdit(ToppingDto? dto = null)
    {
        InitializeComponent();
        _errorHandler = new WpfErrorHandler(ErrorTextBlock);
        _isEdit = dto != null;
        Data = dto ?? new ToppingDto();
        _ = LoadFormAsync();
    }

    private async Task LoadFormAsync()
    {
        TenTextBox.Text = Data.Ten;

        try
        {
            var res = await ApiClient.GetAsync("/api/nhomsanpham");
            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync();
                throw new Exception($"API lỗi {(int)res.StatusCode}: {msg}");
            }

            var result = await res.Content.ReadFromJsonAsync<Result<List<NhomSanPhamDto>>>();
            if (result?.IsSuccess != true || result.Data == null)
                throw new Exception(result?.Message ?? "Không thể tải nhóm sản phẩm.");

            var ds = result.Data;

            _bindingList = ds.Select(x => new NhomSanPhamCheckItem
            {
                Id = x.Id,
                Ten = x.Ten,
                IsChecked = Data.IdNhomSanPham?.Contains(x.Id) == true
            }).ToList();

            NhomSanPhamListBox.ItemsSource = _bindingList;
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, "Tải nhóm sản phẩm");
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _errorHandler.Clear();
        SaveButton.IsEnabled = false;
        Mouse.OverrideCursor = Cursors.Wait;

        try
        {
            if (string.IsNullOrWhiteSpace(TenTextBox.Text))
                throw new Exception("Tên topping là bắt buộc.");

            Data.Ten = TenTextBox.Text.Trim();
            Data.IdNhomSanPham = _bindingList
                .Where(x => x.IsChecked)
                .Select(x => x.Id)
                .ToList();

            HttpResponseMessage response = _isEdit
                ? await ApiClient.PutAsync($"/api/topping/{Data.Id}", Data)
                : await ApiClient.PostAsync("/api/topping", Data);

            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                throw new Exception($"API lỗi {(int)response.StatusCode}: {msg}");
            }

            var result = await response.Content.ReadFromJsonAsync<Result<ToppingDto>>();
            if (result?.IsSuccess != true)
                throw new Exception(result?.Message ?? "Lưu topping thất bại.");

            DialogResult = true;
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, "Lưu topping");
        }
        finally
        {
            SaveButton.IsEnabled = true;
            Mouse.OverrideCursor = null;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            SaveButton_Click(SaveButton, new RoutedEventArgs());
        else if (e.Key == Key.Escape)
            DialogResult = false;
    }

    private void NhomSanPhamListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Ngăn việc chọn dòng
        NhomSanPhamListBox.SelectedIndex = -1;
    }

    private void NhomSanPhamListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement element && element.DataContext is object item)
        {
            var type = item.GetType();
            var prop = type.GetProperty("IsChecked");
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                bool current = (bool)(prop.GetValue(item) ?? false);
                prop.SetValue(item, !current);
            }
        }
    }
}

public class NhomSanPhamCheckItem
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public bool IsChecked { get; set; }
}