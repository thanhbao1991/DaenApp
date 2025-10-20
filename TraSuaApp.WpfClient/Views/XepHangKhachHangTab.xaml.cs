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
    public partial class XepHangKhachHangTab : UserControl, INotifyPropertyChanged
    {
        // === Debounce giống Dashboard ===
        private readonly DebounceDispatcher _debouncer = new();
        private void DebounceSearch(TextBox tb, string key, Action apply, int ms = 300)
            => _debouncer.Debounce(ms, apply);
        // =================================

        private readonly DashboardApi _api = new();
        private bool _isLoading;

        // 🟟 Giới hạn năm tối đa = năm hiện tại để khoá NextYear
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
                    if (IsLoaded) UpdateYearNavButtons();
                }
            }
        }

        public ObservableCollection<KhachHangXepHangDto> Items { get; } = new();

        private ObservableCollection<KhachHangXepHangDto> _filteredItems = new();
        public ObservableCollection<KhachHangXepHangDto> FilteredItems
        {
            get => _filteredItems;
            set { _filteredItems = value; OnPropertyChanged(nameof(FilteredItems)); }
        }

        public XepHangKhachHangTab()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += XepHangKhachHangTab_Loaded;
        }

        private async void XepHangKhachHangTab_Loaded(object? sender, RoutedEventArgs e)
        {
            UpdateYearNavButtons(); // 🟟 set trạng thái nút ngay khi load
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
            if (CurrentYear >= _maxYear) return; // 🟟 chặn vượt năm hiện tại

            CurrentYear++;
            await LoadData();
            UpdateYearNavButtons();
        }

        private void UpdateYearNavButtons()
        {
            if (NextYearButton != null)
                NextYearButton.IsEnabled = CurrentYear < _maxYear;
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
                    var result = await _api.GetXepHangKhachHang(CurrentYear);
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
                        TongTextBlock.Header = "Không có dữ liệu.";
                    }
                }
                finally { _isLoading = false; }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
                DebounceSearch(tb, "XepHangKhachHang", ApplyFilter, 300);
        }

        private void ApplyFilter()
        {
            var kw = (SearchBox.Text ?? string.Empty).MyNormalizeText();

            IEnumerable<KhachHangXepHangDto> src = Items;
            if (!string.IsNullOrWhiteSpace(kw))
            {
                src = src.Where(x =>
                    (!string.IsNullOrEmpty(x.TenKhachHang) && x.TenTimKiem.Contains(kw)) ||
                    (!string.IsNullOrEmpty(x.SoDienThoai) && x.SoDienThoai!.MyNormalizeText().Contains(kw)));
            }

            int stt = 1;
            var list = src.Select(x => new KhachHangXepHangDto
            {
                KhachHangId = x.KhachHangId,
                TenKhachHang = x.TenKhachHang,
                SoDienThoai = x.SoDienThoai,
                TongSoDon = x.TongSoDon,
                TongDoanhThu = x.TongDoanhThu,
                LanCuoiMua = x.LanCuoiMua,
                Stt = stt++
            }).ToList();

            FilteredItems = new ObservableCollection<KhachHangXepHangDto>(list);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}