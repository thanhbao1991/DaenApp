using System.Diagnostics;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;

namespace TraSuaApp.WpfClient.Views
{
    public partial class ChiTieuHangNgayTab : UserControl
    {
        private readonly DebounceManager _debouncer = new();
        private readonly WpfErrorHandler _errorHandler = new();

        private List<ChiTieuHangNgayDto> _fullChiTieuHangNgayList = new();

        public DateTime Today { get; set; } = DateTime.Today;

        public ChiTieuHangNgayDto? SelectedChiTieu
            => ChiTieuHangNgayDataGrid.SelectedItem as ChiTieuHangNgayDto;

        public ChiTieuHangNgayTab()
        {
            InitializeComponent();

            Loaded += async (_, __) =>
            {
                await EnsureChiTieuModuleReadyAsync();
                await ReloadUI();
            };
        }

        public void TriggerAddNew()
        {
        }

        private static async Task EnsureChiTieuModuleReadyAsync()
        {
            var sw = Stopwatch.StartNew();

            while ((AppProviders.ChiTieuHangNgays == null ||
                    AppProviders.NguyenLieus == null)
                   && sw.ElapsedMilliseconds < 5000)
            {
                await Task.Delay(50);
            }

            var tasks = new List<Task>(2);

            if (AppProviders.ChiTieuHangNgays!.Items.Count == 0)
                tasks.Add(AppProviders.ChiTieuHangNgays.InitializeAsync());

            if (AppProviders.NguyenLieus!.Items.Count == 0)
                tasks.Add(AppProviders.NguyenLieus.InitializeAsync());

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }

        public async Task ReloadUI()
        {
            var todayLocal = Today.AddDays(0);

            _fullChiTieuHangNgayList =
                await UiListHelper.BuildListAsync(
                    AppProviders.ChiTieuHangNgays.Items.Where(x => !x.IsDeleted),
                    snap => snap
                        .Where(x => x.Ngay == todayLocal)
                        .OrderByDescending(x => x.NgayGio)
                        .ToList());

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string keyword =
                (SearchChiTieuHangNgayTextBox.Text ?? "")
                .Trim()
                .ToLower();

            var sourceList =
                string.IsNullOrWhiteSpace(keyword)
                    ? _fullChiTieuHangNgayList
                    : _fullChiTieuHangNgayList
                        .Where(x => x.TimKiem.ToLower().Contains(keyword))
                        .ToList();

            int stt = 1;
            foreach (var item in sourceList)
                item.Stt = stt++;

            ChiTieuHangNgayDataGrid.ItemsSource = sourceList;

            var tongTien = sourceList.Sum(x => x.ThanhTien);

            //   TongTienChiTieuHangNgayTextBlock.Header = $"{tongTien:N0}";
        }

        private void SearchChiTieuHangNgayTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            _debouncer.Debounce(
                "ChiTieuHangNgay",
                300,
                ApplyFilter);
        }

        private async void AddChiTieuHangNgayButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var owner = Window.GetWindow(this);

            var window = new ChiTieuHangNgayEdit
            {
                Width = owner?.ActualWidth ?? 1200,
                Height = owner?.ActualHeight ?? 800,
                Owner = owner
            };

            if (window.ShowDialog() == true)
                await ReloadAfterHoaDonChangeAsync();
        }

        private async void AddChiTieuHangNgayButtonN_Click(
            object sender,
            RoutedEventArgs e)
        {
            var owner = Window.GetWindow(this);

            var window = new ChiTieuHangNgayEditN
            {
                Width = owner?.ActualWidth ?? 1200,
                Height = owner?.ActualHeight ?? 800,
                Owner = owner
            };

            if (window.ShowDialog() == true)
                await ReloadAfterHoaDonChangeAsync();
        }

        private async void ChiTieuHangNgayDataGrid_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (ChiTieuHangNgayDataGrid.SelectedItem
                is not ChiTieuHangNgayDto selected)
                return;

            var window = new ChiTieuHangNgayEdit(selected)
            {
                Owner = Window.GetWindow(this)
            };

            if (window.ShowDialog() == true)
                await ReloadAfterHoaDonChangeAsync();
        }

        private async void XoaChiTieuHangNgayButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (ChiTieuHangNgayDataGrid.SelectedItem
                is not ChiTieuHangNgayDto selected)
                return;

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response =
                    await ApiClient.DeleteAsync(
                        $"/api/ChiTieuHangNgay/{selected.Id}");

                var result =
                    await response.Content.ReadFromJsonAsync
                        <Result<ChiTieuHangNgayDto>>();

                if (result?.IsSuccess == true)
                    await ReloadAfterHoaDonChangeAsync();
                else
                    _errorHandler.Handle(
                        new Exception(result?.Message ?? "Không thể xoá."),
                        "Delete");
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

        private async Task ReloadAfterHoaDonChangeAsync(
            bool reloadHoaDon = false,
            bool reloadThanhToan = false,
            bool reloadNo = false,
            bool reloadChiTieu = true)
        {
            if (reloadHoaDon)
                await AppProviders.HoaDons.ReloadAsync();

            if (reloadThanhToan)
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();

            if (reloadChiTieu)
                await AppProviders.ChiTieuHangNgays.ReloadAsync();

            await ReloadUI();
        }
    }
}