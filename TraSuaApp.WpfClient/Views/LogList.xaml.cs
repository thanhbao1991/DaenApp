using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views;

public partial class LogList : Window
{
    private List<LogDto> _all = new();
    private readonly WpfErrorHandler _errorHandler = new();

    public LogList()
    {
        InitializeComponent();
        _ = LoadAsync();
        this.PreviewKeyDown += LogList_PreviewKeyDown;
    }

    private void LogList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            ReloadButton_Click(null!, null!);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CloseButton_Click(null!, null!);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (LogDataGrid.SelectedItem is LogDto selected)
            {
                ShowLogDetailPopup(selected);
                e.Handled = true;
            }
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
            var response = await ApiClient.GetAsync($"/api/logs");
            if (!response.IsSuccessStatusCode)
            {
                var msg = await response.Content.ReadAsStringAsync();
                _errorHandler.Handle(new Exception(msg), "Tải log");
                return;
            }

            var paged = await response.Content.ReadFromJsonAsync<PagedResultDto<LogDto>>();
            if (paged?.Items != null)
            {
                _all = paged.Items.OrderByDescending(x => x.ThoiGian).ToList();
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
        var keyword = SearchTextBox.Text.Trim().ToLower();

        var filtered = _all
            .Where(x =>
                (x.Path ?? "").ToLower().Contains(keyword) ||
                (x.UserName ?? "").ToLower().Contains(keyword) ||
                (x.Method ?? "").ToLower().Contains(keyword))
            .ToList();

        for (int i = 0; i < filtered.Count; i++)
            filtered[i].STT = i + 1;

        LogDataGrid.ItemsSource = filtered;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplySearch();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void LogDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var row = ItemsControl.ContainerFromElement(LogDataGrid, e.OriginalSource as DependencyObject) as DataGridRow;
        if (row == null) return;

        if (row.Item is LogDto selected)
        {
            var response = await ApiClient.GetAsync($"/api/logs/{selected.Id}");
            if (!response.IsSuccessStatusCode) return;

            var detail = await response.Content.ReadFromJsonAsync<LogDto>();
            if (detail != null)
                ShowLogDetailPopup(detail);
        }
    }

    private void ShowLogDetailPopup(LogDto log)
    {
        MessageBox.Show(
            $"🟟 {log.ThoiGian:yyyy-MM-dd HH:mm:ss}\n\n" +
            $"🟟 User: {log.UserName}\n" +
            $"🟟 Path: {log.Method} {log.Path}\n" +
            $"🟟 IP: {log.IP}\n" +
            $"🟟 Request:\n{log.RequestBody}\n\n" +
            $"🟟 Response:\n{log.ResponseBody}\n\n" +
            $"⏱ Duration: {log.DurationMs}ms\n" +
            (string.IsNullOrEmpty(log.ExceptionMessage) ? "" : $"\n❗ Exception:\n{log.ExceptionMessage}"),
            "Chi tiết log",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}