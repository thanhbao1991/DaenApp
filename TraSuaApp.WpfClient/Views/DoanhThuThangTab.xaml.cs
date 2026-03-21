using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class DoanhThuThangTab : UserControl
    {
        private readonly DoanhThuApi _api = new();
        public ObservableCollection<DoanhThuThangItemDto> DoanhThuTheoNgay { get; set; }
            = new ObservableCollection<DoanhThuThangItemDto>();

        public int Thang { get; set; } = DateTime.Today.Month;
        public int Nam { get; set; } = DateTime.Today.Year;

        public decimal TongDoanhThu { get; set; }
        public decimal TongChiTieu { get; set; }
        public decimal TongSoDon { get; set; }
        public decimal LoiNhuan => TongDoanhThu - TongChiTieu;

        public string ThangNamText => $"Tháng {Thang}/{Nam}";

        public DoanhThuThangTab()
        {
            InitializeComponent();
            DataContext = this;
            _ = LoadData();
        }

        private async Task LoadData()
        {
            var res = await _api.GetDoanhThuThang(Thang, Nam);
            if (!res.IsSuccess)
            {
                MessageBox.Show(res.Message);
                return;
            }

            var data = res.Data ?? new List<DoanhThuThangItemDto>();

            // ===== TÍNH TỔNG =====
            var tong = new DoanhThuThangItemDto
            {
                Ngay = DateTime.MinValue, // đánh dấu dòng tổng

                SoDon = data.Sum(x => x.SoDon),
                TongTien = data.Sum(x => x.TongTien),
                ChiTieu = data.Sum(x => x.ChiTieu),
                TongTienMat = data.Sum(x => x.TongTienMat),
                TienBank = data.Sum(x => x.TienBank),
                TienNo = data.Sum(x => x.TienNo),

                TaiCho = data.Sum(x => x.TaiCho),
                MuaVe = data.Sum(x => x.MuaVe),
                DiShip = data.Sum(x => x.DiShip),
                AppShipping = data.Sum(x => x.AppShipping),

                ThuongNha = data.Sum(x => x.ThuongNha),
                ThuongKhanh = data.Sum(x => x.ThuongKhanh)
            };

            data.Add(tong);

            Datagrid.ItemsSource = data;
        }
        private async void PrevMonth_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Thang == 1)
            {
                Thang = 12;
                Nam--;
            }
            else
            {
                Thang--;
            }

            await LoadData();
        }

        private async void NextMonth_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (Thang == 12)
            {
                Thang = 1;
                Nam++;
            }
            else
            {
                Thang++;
            }

            await LoadData();
        }





















    }
}