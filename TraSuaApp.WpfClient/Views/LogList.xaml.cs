using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views;

public partial class LogList : Window
{
    private List<LogDto> _logs = new();
    private readonly WpfErrorHandler _errorHandler = new();
    private DateTime _currentDate = DateTime.Today;
    private Guid? _entityIdFilter = null;

    public LogList()
    {
        InitializeComponent();
        _ = LoadByDateAsync(_currentDate);
        this.PreviewKeyDown += LogList_PreviewKeyDown;
    }

    private async Task LoadByDateAsync(DateTime date)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            //var url = $"/api/log/date/{date:yyyy-MM-dd}";
            var url = $"/api/log/by-date?ngay={date:yyyy-MM-dd}";
            var response = await ApiClient.GetAsync(url);
            var result = await response.Content.ReadFromJsonAsync<Result<List<LogDto>>>();

            if (result?.IsSuccess != true || result.Data == null)
                throw new Exception(result?.Message ?? "Không thể tải log theo ngày.");

            _logs = result.Data.OrderByDescending(x => x.ThoiGian).ToList();

            foreach (var log in _logs)
                log.KetQua = LogHelper.RutGonLog(log);

            ApplySearch();
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, "LoadByDateAsync");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private async Task LoadByEntityIdAsync(Guid entityId)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;

            var response = await ApiClient.GetAsync($"/api/log/by-entity?entityId={entityId}");
            var result = await response.Content.ReadFromJsonAsync<Result<List<LogDto>>>();

            if (result?.IsSuccess != true || result.Data == null)
                throw new Exception(result?.Message ?? "Không thể truy vết theo EntityId.");

            _logs = result.Data.OrderByDescending(x => x.ThoiGian).ToList();
            _entityIdFilter = entityId;

            foreach (var log in _logs)
                log.KetQua = LogHelper.RutGonLog(log);

            ApplySearch();
        }
        catch (Exception ex)
        {
            _errorHandler.Handle(ex, "LoadByEntityIdAsync");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void ApplySearch()
    {
        var keyword = SearchTextBox.Text.Trim();
        var filtered = TextSearchHelper.FilterByTen(_logs, keyword, x => x.KetQua);

        for (int i = 0; i < filtered.Count; i++)
            filtered[i].STT = i + 1;

        LogDataGrid.ItemsSource = filtered;
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplySearch();
    }

    private void LogDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LogDataGrid.SelectedItem is not LogDto selected) return;

        string chiTiet = LogHelper.ChiTietLog(selected);
        MessageBox.Show(chiTiet, "Chi tiết log");
    }

    private void ReloadButton_Click(object sender, RoutedEventArgs e)
    {
        if (_entityIdFilter.HasValue)
            _ = LoadByEntityIdAsync(_entityIdFilter.Value);
        else
            _ = LoadByDateAsync(_currentDate);
    }

    private void TraceButton_Click(object sender, RoutedEventArgs e)
    {
        if (LogDataGrid.SelectedItem is not LogDto selected || selected.EntityId == null) return;
        _ = LoadByEntityIdAsync(selected.EntityId.Value);
    }

    private void PreviousDayButton_Click(object sender, RoutedEventArgs e)
    {
        _entityIdFilter = null;
        _currentDate = _currentDate.AddDays(-1);
        _ = LoadByDateAsync(_currentDate);
    }

    private void NextDayButton_Click(object sender, RoutedEventArgs e)
    {
        _entityIdFilter = null;
        _currentDate = _currentDate.AddDays(1);
        _ = LoadByDateAsync(_currentDate);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void LogList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            ReloadButton_Click(null!, null!);
            e.Handled = true;
        }
    }
}