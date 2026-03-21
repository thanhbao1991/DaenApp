using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class DoanhThuNamTab : UserControl
    {
        private readonly DoanhThuApi _api = new();

        public DoanhThuNamTab()
        {
            InitializeComponent();
            InitNam();
            LoadData();
        }

        private void InitNam()
        {
            var currentYear = DateTime.Now.Year;

            var years = new List<int>();
            for (int i = currentYear - 1; i <= currentYear + 0; i++)
                years.Add(i);

            cmbNam.ItemsSource = years;
            cmbNam.SelectedItem = currentYear;
        }

        private async void LoadData()
        {
            if (cmbNam.SelectedItem == null) return;

            int nam = (int)cmbNam.SelectedItem;

            try
            {
                var res = await _api.GetDoanhThuNam(nam);

                if (!res.IsSuccess)
                {
                    MessageBox.Show(res.Message);
                    return;
                }

                var data = res.Data ?? new List<DoanhThuNamItemDto>();

                // Thêm dòng tổng cuối năm
                var tong = new DoanhThuNamItemDto
                {
                    Thang = 0,
                    SoDon = data.Sum(x => x.SoDon),
                    TongTien = data.Sum(x => x.TongTien),
                    ChiTieu = data.Sum(x => x.ChiTieu),
                    TongTienMat = data.Sum(x => x.TongTienMat),
                    TienBank = data.Sum(x => x.TienBank),
                    TienNo = data.Sum(x => x.TienNo),
                    TaiCho = data.Sum(x => x.TaiCho),
                    MuaVe = data.Sum(x => x.MuaVe),
                    DiShip = data.Sum(x => x.DiShip),
                    AppShipping = data.Sum(x => x.AppShipping)
                };

                data.Add(tong);

                dataGrid.ItemsSource = data;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }
    }
}