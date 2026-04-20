using System.Diagnostics;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.WpfClient.DataProviders;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Services;
using TraSuaApp.WpfClient.SettingsViews;

namespace TraSuaApp.WpfClient.Views
{
    public partial class CongViecNoiBoTab : UserControl
    {
        private readonly DebounceManager _debouncer = new();
        private readonly WpfErrorHandler _errorHandler = new();

        private List<CongViecNoiBoDto> _fullCongViecNoiBoList = new();

        public CongViecNoiBoTab()
        {
            InitializeComponent();

            Loaded += async (_, __) =>
            {
                var sw = Stopwatch.StartNew();
                while (AppProviders.CongViecNoiBos == null && sw.ElapsedMilliseconds < 5000)
                    await Task.Delay(100);

                await ReloadUI();
            };
        }

        public async Task ReloadUI()
        {
            if (AppProviders.CongViecNoiBos == null) return;

            _fullCongViecNoiBoList = await UiListHelper.BuildListAsync(
                AppProviders.CongViecNoiBos.Items,
                snap => snap
                            .OrderBy(x => x.DaHoanThanh)
                            .ThenByDescending(x => x.LastModified)
                            .ToList()
            );

            ApplyCongViecNoiBoFilter();
        }

        private void ApplyCongViecNoiBoFilter()
        {
            string keyword = SearchCongViecNoiBoTextBox.Text.Trim().ToLower();

            var sourceList = string.IsNullOrWhiteSpace(keyword)
                ? _fullCongViecNoiBoList
                : _fullCongViecNoiBoList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();

            int stt = 1;
            foreach (var item in sourceList)
                item.Stt = stt++;

            CongViecNoiBoDataGrid.ItemsSource = sourceList;
        }

        private void SearchCongViecNoiBoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debouncer.Debounce("CongViecNoiBo", 300, ApplyCongViecNoiBoFilter);
        }

        private async void AddCongViecNoiBoButton_Click(object sender, RoutedEventArgs e)
        {
            var owner = Window.GetWindow(this);
            var window = new CongViecNoiBoEdit()
            {
                Width = owner?.ActualWidth ?? 1200,
                Height = owner?.ActualHeight ?? 800,
                Owner = owner
            };

            if (window.ShowDialog() == true)
                await AppProviders.CongViecNoiBos.ReloadAsync();
        }

        private async void SuaCongViecNoiBoButton_Click(object sender, RoutedEventArgs e)
        {
            if (CongViecNoiBoDataGrid.SelectedItem is not CongViecNoiBoDto selected) return;

            var owner = Window.GetWindow(this);
            var window = new CongViecNoiBoEdit(selected)
            {
                Width = owner?.ActualWidth ?? 1200,
                Height = owner?.ActualHeight ?? 800,
                Owner = owner
            };

            if (window.ShowDialog() == true)
                await AppProviders.CongViecNoiBos.ReloadAsync();
        }

        private async void XoaCongViecNoiBoButton_Click(object sender, RoutedEventArgs e)
        {
            if (CongViecNoiBoDataGrid.SelectedItem is not CongViecNoiBoDto selected)
                return;

            var confirm = MessageBox.Show(
               $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
               "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response = await ApiClient.DeleteAsync($"/api/CongViecNoiBo/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<CongViecNoiBoDto>>();

                if (result?.IsSuccess == true)
                {
                    AppProviders.CongViecNoiBos.Remove(selected.Id);
                }
                else
                {
                    _errorHandler.Handle(new Exception(result?.Message ?? "Không thể xoá."), "Delete");
                }
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

        private async void CongViecNoiBoDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CongViecNoiBoDataGrid.SelectedItem is not CongViecNoiBoDto selected) return;

            var api = Apis.CongViecNoiBo;

            // ===== TOGGLE =====
            selected.DaHoanThanh = !selected.DaHoanThanh;
            selected.NgayGio = DateTime.Now;

            if (selected.DaHoanThanh)
            {
                if (selected.XNgayCanhBao != null && selected.XNgayCanhBao != 0)
                    selected.NgayCanhBao = selected.NgayGio.Value.AddDays(selected.XNgayCanhBao ?? 0);
            }
            else
            {
                selected.NgayCanhBao = null;
            }

            var result = await api.UpdateAsync(selected.Id, selected);

            if (!result.IsSuccess)
            {
                NotiHelper.ShowError($"Lỗi: {result.Message}");
                return;
            }

            var updated = result.Data!;

            selected.DaHoanThanh = updated.DaHoanThanh;
            selected.NgayGio = updated.NgayGio;
            selected.NgayCanhBao = updated.NgayCanhBao;
            selected.LastModified = updated.LastModified;

            SearchCongViecNoiBoTextBox.Text = "";
            await ReloadUI();
            SearchCongViecNoiBoTextBox.Focus();
        }
    }
}