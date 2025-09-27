using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class ChiTieuNhaTab : UserControl
    {
        private readonly ChiTieuHangNgayApi _api = new ChiTieuHangNgayApi();
        public ObservableCollection<ChiTieuHangNgayDto> Items { get; } = new();

        // offset tháng: 0 = tháng này, -1 = tháng trước, -2 = 2 tháng trước...
        private int _currentOffset = 0;

        private bool _isLoading = false;
        private bool _didInitialLoad = false;

        public ChiTieuNhaTab()
        {
            InitializeComponent();
            ChiTieuHangNgayNhaDataGrid.ItemsSource = Items;

            Loaded += ChiTieuNhaTab_Loaded;
            Unloaded += ChiTieuNhaTab_Unloaded;

            // Tự refresh khi tab hiển thị lại (Cách B)
            IsVisibleChanged += ChiTieuNhaTab_IsVisibleChanged;

            UpdateMonthLabel();
            UpdateNavButtons();
        }

        private void ChiTieuNhaTab_Loaded(object sender, RoutedEventArgs e)
        {
            // Không gọi LoadData ở đây để tránh double; IsVisibleChanged sẽ lo
        }

        private void ChiTieuNhaTab_Unloaded(object sender, RoutedEventArgs e)
        {
            // no-op
        }

        // Khi tab hiển thị: lần đầu + mỗi lần quay lại
        private async void ChiTieuNhaTab_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible) return;

            if (!_didInitialLoad)
            {
                await LoadData(_currentOffset);
                _didInitialLoad = true;
            }
            else
            {
                await LoadData(_currentOffset);
            }
        }

        // ===== NAVIGATION =====
        private async void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            _currentOffset -= 1; // đi về quá khứ
            UpdateMonthLabel();
            UpdateNavButtons();
            await LoadData(_currentOffset);
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOffset < 0)
            {
                _currentOffset += 1; // tiến gần hiện tại
                UpdateMonthLabel();
                UpdateNavButtons();
                await LoadData(_currentOffset);
            }
        }

        private void UpdateMonthLabel()
        {
            var today = DateTime.Today;
            var firstThis = new DateTime(today.Year, today.Month, 1);
            var month = firstThis.AddMonths(_currentOffset);
            MonthLabel.Text = $"{month:MM/yyyy}";
        }

        private void UpdateNavButtons()
        {
            // Không cho vượt tương lai
            NextButton.IsEnabled = _currentOffset < 0;
        }

        // ===== RELOAD =====
        public Task RefreshAsync() => LoadData(_currentOffset);

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadData(_currentOffset);
        }

        // ===== LOAD DATA =====
        private async Task LoadData(int offset)
        {
            if (_isLoading) return;
            _isLoading = true;

            BeginLoadingUI();
            try
            {
                // Tính năm/tháng theo offset và gọi API theo năm/tháng
                var (year, month) = GetYearMonthByOffset(offset);
                Result<System.Collections.Generic.List<ChiTieuHangNgayDto>> result =
                    await _api.GetByNguyenLieuInMonth(year, month);

                Items.Clear();

                if (result.IsSuccess && result.Data != null)
                {
                    // Mới nhất lên đầu
                    var sorted = result.Data.OrderByDescending(x => x.Ngay).ToList();
                    int stt = 1;
                    foreach (var item in sorted)
                    {
                        item.Stt = stt++;
                        Items.Add(item);
                    }

                    decimal tongTien = Items.Sum(x => x.ThanhTien);
                    TongTienTextBlock.Text = $"Tổng tiền: {tongTien:N0} đ";
                }
                else
                {
                    TongTienTextBlock.Text = "Tổng tiền: 0 đ";
                }
            }
            finally
            {
                EndLoadingUI();
                _isLoading = false;
            }
        }

        private static (int year, int month) GetYearMonthByOffset(int offset)
        {
            var today = DateTime.Today;
            var firstThis = new DateTime(today.Year, today.Month, 1);
            var m = firstThis.AddMonths(offset);
            return (m.Year, m.Month);
        }

        private void BeginLoadingUI()
        {
            ReloadButton.IsEnabled = false;          // ButtonWithSpinnerStyle sẽ hiện spinner
            PrevButton.IsEnabled = false;
            NextButton.IsEnabled = false;
            ChiTieuHangNgayNhaDataGrid.IsEnabled = false;
        }

        private void EndLoadingUI()
        {
            ReloadButton.IsEnabled = true;
            PrevButton.IsEnabled = true;
            UpdateNavButtons();                       // giữ rule không vượt tương lai
            ChiTieuHangNgayNhaDataGrid.IsEnabled = true;
        }
    }
}