using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class ChiTieuTab : UserControl
    {
        private readonly DashboardApi _api = new();
        public ObservableCollection<ChiTieuHangNgayDto> Items { get; } = new();

        private int _currentOffset = 0;
        private bool _isLoading;
        private bool _didInitialLoad;
        private bool _ungNha;

        public ChiTieuTab(bool ungNha)
        {
            InitializeComponent();
            ChiTieuDataGrid.ItemsSource = Items;
            Loaded += ChiTieuNguyenLieuTab_Loaded;
            IsVisibleChanged += ChiTieuTab_IsVisibleChanged;
            UpdateMonthLabel();
            UpdateNavButtons();
            _ungNha = ungNha;
        }

        private async void ChiTieuNguyenLieuTab_Loaded(object sender, RoutedEventArgs e)
        {
            await EnsureProvidersReadyAsync();

            // 🟟 Gán danh sách nguyên liệu (chuẩn hóa)
            var list = AppProviders.NguyenLieus?.Items?
                .OrderBy(x => x.Ten, StringComparer.CurrentCultureIgnoreCase)
                .ToList() ?? new();

            // Thêm “Tất cả”
            // list.Insert(0, new NguyenLieuDto { Id = Guid.Empty, Ten = "Tất cả" });

            NguyenLieuBox.NguyenLieuList = list;

            // Chọn mặc định nguyên liệu cố định
            if (_ungNha)
                NguyenLieuBox.SetSelectedNguyenLieuByIdWithoutPopup(
        Guid.Parse("7995B334-44D1-4768-89C7-280E6B0413AE")
                    );
        }

        private async Task EnsureProvidersReadyAsync()
        {
            for (int i = 0; i < 50; i++)
            {
                if (AppProviders.NguyenLieus?.Items != null)
                    return;
                await Task.Delay(100);
            }
        }

        private async void ChiTieuTab_IsVisibleChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible) return;
            if (_ungNha)
            {
                await LoadData(_currentOffset,
            Guid.Parse("7995B334-44D1-4768-89C7-280E6B0413AE")

                    );
                NguyenLieuBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                NguyenLieuBox.Visibility = Visibility.Visible;
                await LoadData(_currentOffset);

            }
        }

        // 🟟 Khi chọn nguyên liệu mới → tải dữ liệu
        private async void NguyenLieuBox_NguyenLieuSelected(NguyenLieuDto obj)
        {
            await LoadData(_currentOffset, obj.Id);
        }

        // 🟟 Khi xoá chọn → xem tất cả
        private async void NguyenLieuBox_NguyenLieuCleared()
        {
            await LoadData(_currentOffset, Guid.Empty);
        }

        private async Task LoadData(int offset, Guid? fixedNguyenLieuId = null)
        {
            if (_isLoading) return;
            _isLoading = true;

            using (BusyUI.Scope(this, ReloadButton, "Đang tải..."))
            {
                try
                {
                    var selected = fixedNguyenLieuId
                        ?? NguyenLieuBox.SelectedNguyenLieu?.Id
                        ?? Guid.Empty;

                    var result = await _api.GetChiTieuByNguyenLieuId(offset, selected);

                    Items.Clear();
                    if (result.IsSuccess && result.Data != null)
                    {
                        int stt = 1;
                        foreach (var item in result.Data.OrderByDescending(x => x.Ngay))
                        {
                            item.Stt = stt++;
                            Items.Add(item);
                        }

                        TongTienTextBlock.Header = $"{Items.Sum(x => x.ThanhTien):N0} đ";
                    }
                    else
                        TongTienTextBlock.Header = "0 đ";
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