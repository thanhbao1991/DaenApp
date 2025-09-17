using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class ChiTieuHangNgayNhaTab : UserControl
    {
        private readonly ChiTieuHangNgayApi _api = new ChiTieuHangNgayApi();

        public ObservableCollection<ChiTieuHangNgayDto> Items { get; set; } = new();

        public ChiTieuHangNgayNhaTab()
        {
            InitializeComponent();
            ChiTieuHangNgayNhaDataGrid.ItemsSource = Items;
            Loaded += ChiTieuHangNgayNhaTab_Loaded;
        }

        private async void ChiTieuHangNgayNhaTab_Loaded(object sender, RoutedEventArgs e)
        {
            MonthSelector.SelectedIndex = 0; // mặc định chọn "Tháng này"
            await LoadData(0);
        }

        private async void MonthSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonthSelector.SelectedItem is ComboBoxItem selected &&
                int.TryParse(selected.Tag.ToString(), out int offset))
            {
                await LoadData(offset);
            }
        }

        private async Task LoadData(int offset)
        {
            Result<System.Collections.Generic.List<ChiTieuHangNgayDto>> result;

            if (offset == 0)
                result = await _api.GetThisMonth();
            else
                result = await _api.GetLastMonth();

            Items.Clear();

            if (result.IsSuccess && result.Data != null)
            {
                var sorted = result.Data.OrderBy(x => x.Ngay).ToList();

                // đánh STT
                int stt = 1;
                foreach (var item in sorted)
                {
                    item.Stt = stt++;
                    Items.Add(item);
                }

                // tính tổng tiền của đúng tháng đó
                decimal tongTien = Items.Sum(x => x.ThanhTien);
                TongTienTextBlock.Text = $"Tổng tiền: {tongTien:N0} đ";
            }
            else
            {
                TongTienTextBlock.Text = "Tổng tiền: 0 đ";
            }
        }
    }
}