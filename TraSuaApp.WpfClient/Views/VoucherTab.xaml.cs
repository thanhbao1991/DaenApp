using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class VoucherTab : UserControl
    {
        private readonly VoucherApi _api = new VoucherApi();
        public ObservableCollection<VoucherChiTraDto> Items { get; } = new();

        // offset tháng: 0 = tháng này, -1 = tháng trước, -2 = 2 tháng trước... (không cho > 0)
        private int _currentOffset = 0;

        private bool _isLoading = false;
        private bool _didInitialLoad = false;

        public VoucherTab()
        {
            InitializeComponent();
            VoucherDataGrid.ItemsSource = Items;

            Loaded += VoucherTab_Loaded;
            Unloaded += VoucherTab_Unloaded;

            // Tự refresh khi tab hiển thị lại (Cách B như ChiTieuNhaTab)
            IsVisibleChanged += VoucherTab_IsVisibleChanged;

            UpdateMonthLabel();
            UpdateNavButtons();
        }

        private void VoucherTab_Loaded(object sender, RoutedEventArgs e)
        {
            // Không gọi LoadData ở đây để tránh double; IsVisibleChanged sẽ lo
        }

        private void VoucherTab_Unloaded(object sender, RoutedEventArgs e)
        {
            // no-op (không dùng subscription/timer)
        }

        // Hiển thị lần đầu + mỗi lần quay lại tab
        private async void VoucherTab_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
            _currentOffset -= 1;               // đi về quá khứ
            UpdateMonthLabel();
            UpdateNavButtons();
            await LoadData(_currentOffset);
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOffset < 0)
            {
                _currentOffset += 1;           // tiến gần về hiện tại
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
                // Gọi API theo offset (đã triển khai ở VoucherController)
                Result<System.Collections.Generic.List<VoucherChiTraDto>> result =
                    await _api.GetByOffset(offset);

                Items.Clear();

                if (result.IsSuccess && result.Data != null)
                {
                    var sorted = result.Data.OrderByDescending(x => x.Ngay).ToList();
                    int stt = 1;
                    foreach (var item in sorted)
                    {
                        item.Stt = stt++;
                        Items.Add(item);
                    }

                    decimal tong = Items.Sum(x => x.GiaTriApDung);
                    TongTienTextBlock.Text = $"Tổng voucher: {tong:N0} đ";
                }
                else
                {
                    TongTienTextBlock.Text = "Tổng voucher: 0 đ";
                }
            }
            finally
            {
                EndLoadingUI();
                _isLoading = false;
            }
        }

        private void BeginLoadingUI()
        {
            // giống tab chi tiêu: disable để ButtonWithSpinnerStyle tự hiện spinner
            ReloadButton.IsEnabled = false;
            PrevButton.IsEnabled = false;
            NextButton.IsEnabled = false;
            VoucherDataGrid.IsEnabled = false;
        }

        private void EndLoadingUI()
        {
            ReloadButton.IsEnabled = true;
            PrevButton.IsEnabled = true;
            UpdateNavButtons(); // bật/tắt Next theo offset hiện tại
            VoucherDataGrid.IsEnabled = true;
        }
    }
}