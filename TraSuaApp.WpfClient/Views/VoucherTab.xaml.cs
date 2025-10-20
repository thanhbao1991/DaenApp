using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class VoucherTab : UserControl
    {
        private readonly DashboardApi _api = new();
        public ObservableCollection<VoucherChiTraDto> Items { get; } = new();

        private int _currentOffset = 0;
        private bool _isLoading;
        private bool _didInitialLoad;

        public VoucherTab()
        {
            InitializeComponent();
            VoucherDataGrid.ItemsSource = Items;

            Loaded += VoucherTab_Loaded;
            IsVisibleChanged += VoucherTab_IsVisibleChanged;

            UpdateMonthLabel();
            UpdateNavButtons();
        }

        private async void VoucherTab_Loaded(object sender, RoutedEventArgs e)
        {
            await EnsureProvidersReadyAsync();

            var list = AppProviders.Vouchers?.Items?
                .OrderBy(x => x.Ten, StringComparer.CurrentCultureIgnoreCase)
                .ToList() ?? new();

            // 🟟 Thêm “Tất cả” lên đầu
            list.Insert(0, new VoucherDto
            {
                Id = Guid.Empty,
                Ten = "Tất cả"
            });

            VoucherComboBox.ItemsSource = list;
            VoucherComboBox.SelectedIndex = 0;
        }

        private async Task EnsureProvidersReadyAsync()
        {
            for (int i = 0; i < 50; i++)
            {
                if (AppProviders.Vouchers != null && AppProviders.Vouchers.Items != null)
                    return;
                await Task.Delay(100);
            }
        }

        private async void VoucherTab_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible) return;

            if (!_didInitialLoad)
            {
                await LoadData(_currentOffset);
                _didInitialLoad = true;
            }
            else await LoadData(_currentOffset);
        }

        private async void VoucherComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsVisible && _didInitialLoad)
                await LoadData(_currentOffset);
        }

        private async Task LoadData(int offset)
        {
            if (_isLoading) return;
            _isLoading = true;

            using (BusyUI.Scope(this, ReloadButton, "Đang tải..."))
            {
                try
                {
                    var selected = (VoucherComboBox.SelectedItem as VoucherDto)?.Id ?? Guid.Empty;
                    var result = await _api.GetVoucher(offset, selected);

                    Items.Clear();
                    if (result.IsSuccess && result.Data != null)
                    {
                        int stt = 1;
                        foreach (var item in result.Data.OrderByDescending(x => x.Ngay))
                        {
                            item.Stt = stt++;
                            Items.Add(item);
                        }
                        TongTienTextBlock.Header = $"{Items.Sum(x => x.GiaTriApDung):N0} đ";
                    }
                    else TongTienTextBlock.Header = "0 đ";
                }
                finally { _isLoading = false; }
            }
        }

        private async void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            _currentOffset--;
            UpdateMonthLabel();
            await LoadData(_currentOffset);
            UpdateNavButtons();
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOffset < 0)
            {
                _currentOffset++;
                UpdateMonthLabel();
                await LoadData(_currentOffset);
                UpdateNavButtons();
            }
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadData(_currentOffset);
            UpdateNavButtons();
        }

        private void UpdateMonthLabel()
        {
            var today = DateTime.Today;
            var first = new DateTime(today.Year, today.Month, 1);
            var month = first.AddMonths(_currentOffset);
            MonthLabel.Text = $"{month:MM/yyyy}";
        }

        private void UpdateNavButtons() => NextButton.IsEnabled = _currentOffset < 0;
    }
}