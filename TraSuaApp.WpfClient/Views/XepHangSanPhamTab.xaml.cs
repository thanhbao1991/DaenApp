using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class XepHangSanPhamTab : UserControl, INotifyPropertyChanged
    {
        // === Debounce giống Dashboard ===
        private readonly DebounceDispatcher _debouncer = new();
        private void DebounceSearch(TextBox tb, string key, Action apply, int ms = 300)
            => _debouncer.Debounce(ms, apply);
        // =================================

        private readonly DashboardApi _api = new();
        private bool _isLoading;

        // 🟟 Giới hạn năm tối đa = năm hiện tại (để disable NextYearButton)
        private readonly int _maxYear = DateTime.Today.Year;

        private int _currentYear = DateTime.Now.Year;
        public int CurrentYear
        {
            get => _currentYear;
            set
            {
                if (_currentYear != value)
                {
                    _currentYear = value;
                    OnPropertyChanged(nameof(CurrentYear));
                    // Chỉ gọi khi control đã load để tránh null NextYearButton
                    if (IsLoaded) UpdateYearNavButtons();
                }
            }
        }

        public ObservableCollection<SanPhamXepHangDto> Items { get; } = new();

        private ObservableCollection<SanPhamXepHangDto> _filteredItems = new();
        public ObservableCollection<SanPhamXepHangDto> FilteredItems
        {
            get => _filteredItems;
            set { _filteredItems = value; OnPropertyChanged(nameof(FilteredItems)); }
        }

        public XepHangSanPhamTab()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += XepHangSanPhamTab_Loaded;
        }

        private async void XepHangSanPhamTab_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateYearNavButtons(); // 🟟 đảm bảo trạng thái nút ngay khi mở tab
            if (!_isLoading) await LoadData();
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
            UpdateYearNavButtons();
        }

        private async void PrevYearButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentYear--;
            await LoadData();
            UpdateYearNavButtons();
        }

        private async void NextYearButton_Click(object sender, RoutedEventArgs e)
        {
            // 🟟 Không cho vượt quá năm hiện tại
            if (CurrentYear >= _maxYear) return;

            CurrentYear++;
            await LoadData();
            UpdateYearNavButtons();
        }

        private void UpdateYearNavButtons()
        {
            if (NextYearButton != null)
                NextYearButton.IsEnabled = CurrentYear < _maxYear;

            // (tuỳ chọn) nếu muốn chặn lùi quá xa, có thể set PrevYearButton.IsEnabled = CurrentYear > _minYear;
        }

        private async Task LoadData()
        {
            if (_isLoading) return;
            _isLoading = true;

            using (BusyUI.Scope(this, ReloadButton, "Đang tải..."))
            {
                Items.Clear();
                try
                {
                    var result = await _api.GetXepHangSanPham(CurrentYear);
                    if (result.IsSuccess && result.Data != null)
                    {
                        int stt = 1;
                        foreach (var it in result.Data)
                        {
                            it.Stt = stt++;
                            Items.Add(it);
                        }
                        ApplyFilter();
                        TongTextBlock.Header = $"{Items.Count:N0}";
                    }
                    else
                    {
                        FilteredItems.Clear();
                        TongTextBlock.Header = "";
                    }
                }
                finally { _isLoading = false; }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                DebounceSearch(tb, "XepHangSanPham", ApplyFilter, 300);
        }

        private void ApplyFilter()
        {
            var kw = (SearchBox.Text ?? string.Empty).MyNormalizeText();

            IEnumerable<SanPhamXepHangDto> src = Items;
            if (!string.IsNullOrWhiteSpace(kw))
            {
                src = src.Where(x =>
                    !string.IsNullOrEmpty(x.TenSanPham) &&
                    (x.TenTimKiem.Contains(kw) ||
                     kw.Contains(x.TenTimKiem) ||
                     x.TenSanPham.MyNormalizeText().Contains(kw)));
            }

            int stt = 1;
            var list = src.Select(x => new SanPhamXepHangDto
            {
                TenSanPham = x.TenSanPham,
                TongSoLuong = x.TongSoLuong,
                TongDoanhThu = x.TongDoanhThu,
                Stt = stt++
            }).ToList();

            FilteredItems = new ObservableCollection<SanPhamXepHangDto>(list);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}