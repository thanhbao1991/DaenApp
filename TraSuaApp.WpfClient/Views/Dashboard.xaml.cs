using System.Net.Http.Json;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class Dashboard : Window
    {
        public Dashboard()
        {
            InitializeComponent();
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            GenerateMenu("Admin", AdminMenu);
            GenerateMenu("HoaDon", HoaDonMenu);
            GenerateMenu("Settings", SettingsMenu);


            Loaded += Dashboard_Loaded;
        }
        private void GenerateMenu(string loai, MenuItem m)
        {
            var viewType = typeof(Window);
            var views = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(viewType)
                  && t.FullName.Contains(loai)
                  && t.Name.Contains("List")
                )
                .OrderBy(t => t.Name);

            foreach (var view in views)
            {
                var btn = new MenuItem
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Header =
                    TuDien._tableFriendlyNames[view.Name.Replace("List", "").Replace("Edit", "")],
                    Tag = view.Name,
                    ToolTip = loai,
                    //Style = (Style)FindResource("ThemButtonStyle"),
                    Margin = new Thickness(4)
                };
                btn.Click += MenuButton_Click;
                m.Items.Add(btn);
            }
        }

        private async void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem btn || btn.Tag is not string tag) return;

            try
            {
                var namespaceName = $"TraSuaApp.WpfClient.{btn.ToolTip}Views";
                var typeName = $"{namespaceName}.{tag}";
                var type = Type.GetType(typeName);

                if (type == null)
                {
                    MessageBox.Show($"Không tìm thấy form: {tag}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Hiển thị loading (nếu có custom loading dialog riêng)
                // LoadingOverlay.Show();

                await Task.Delay(100); // tạo cảm giác phản hồi nhanh hơn

                if (Activator.CreateInstance(type) is Window window)
                {
                    window.Width = this.Width;
                    window.Height = this.Height;
                    window.Owner = this;
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi mở form '{tag}': {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // LoadingOverlay.Hide();
            }

        }

        private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await ApiClient.GetAsync("/api/dashboard/homnay");
                var dashboard = await result.Content.ReadFromJsonAsync<DashboardDto>();
                if (dashboard == null) return;
                now = DateTime.Today;


                // 4 - Bán nhiều hôm nay
                BanNhieuGrid.ItemsSource = dashboard.TopSanPhams;



                while (AppProviders.CongViecNoiBos?.Items == null)
                {
                    await Task.Delay(100); // chờ 100ms rồi kiểm tra lại
                }
                AppProviders.CongViecNoiBos.OnChanged += ReloadCongViecNoiBoUI;
                await AppProviders.CongViecNoiBos.ReloadAsync();
                ReloadCongViecNoiBoUI();

                while (AppProviders.ChiTietHoaDonNos?.Items == null)
                {
                    await Task.Delay(100); // chờ 100ms rồi kiểm tra lại
                }
                AppProviders.ChiTietHoaDonNos.OnChanged += ReloadChiTietHoaDonNoUI;
                await AppProviders.ChiTietHoaDonNos.ReloadAsync();
                ReloadChiTietHoaDonNoUI();

                while (AppProviders.ChiTietHoaDonThanhToans?.Items == null)
                {
                    await Task.Delay(100); // chờ 100ms rồi kiểm tra lại
                }
                AppProviders.ChiTietHoaDonThanhToans.OnChanged += ReloadChiTietHoaDonThanhToanUI;
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();
                ReloadChiTietHoaDonThanhToanUI();

                while (AppProviders.ChiTieuHangNgays?.Items == null)
                {
                    await Task.Delay(100); // chờ 100ms rồi kiểm tra lại
                }
                AppProviders.ChiTieuHangNgays.OnChanged += ReloadChiTieuHangNgayUI;
                await AppProviders.ChiTieuHangNgays.ReloadAsync();
                ReloadChiTieuHangNgayUI();

                while (AppProviders.HoaDons?.Items == null)
                {
                    await Task.Delay(100); // chờ 100ms rồi kiểm tra lại
                }
                AppProviders.HoaDons.OnChanged += ReloadHoaDonUI;
                await AppProviders.HoaDons.ReloadAsync();
                ReloadHoaDonUI();


                //var result2 = await ApiClient.GetAsync("/api/dashboard/dubao");
                //var dashboard2 = await result2.Content.ReadFromJsonAsync<DashboardDto>();
                //if (dashboard2 == null) return;
                //ParseBoldMarkdown(DuDoanDoanhThu, dashboard2.PredictedPeak);

            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dashboard: " + ex.Message);
            }
        }

        public static void ParseBoldMarkdown(TextBlock textBlock, string input)
        {
            if (input == null) { return; }
            textBlock.Inlines.Clear();

            var lines = input.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                int currentIndex = 0;
                while (currentIndex < line.Length)
                {
                    int startBold = line.IndexOf("**", currentIndex);
                    if (startBold < 0)
                    {
                        // No more bold text
                        textBlock.Inlines.Add(new Run(line.Substring(currentIndex)));
                        break;
                    }

                    int endBold = line.IndexOf("**", startBold + 2);
                    if (endBold < 0)
                    {
                        // No closing **
                        textBlock.Inlines.Add(new Run(line.Substring(currentIndex)));
                        break;
                    }

                    // Normal text before **
                    if (startBold > currentIndex)
                    {
                        textBlock.Inlines.Add(new Run(line.Substring(currentIndex, startBold - currentIndex)));
                    }

                    // Bold text inside **
                    string boldText = line.Substring(startBold + 2, endBold - startBold - 2);
                    textBlock.Inlines.Add(new Run(boldText) { FontWeight = FontWeights.Bold });

                    currentIndex = endBold + 2;
                }

                textBlock.Inlines.Add(new LineBreak());
            }
        }

        // --- Custom Title Bar Logic ---
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // ... (các đoạn code khác giữ nguyên)

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                // Lưu lại kích thước và vị trí ban đầu trước khi maximize
                // ... (nếu cần thiết để khôi phục)

                // Thiết lập MaxHeight và MaxWidth để chừa lại thanh taskbar
                this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

                this.WindowState = WindowState.Maximized;
            }
        }

        // ... (các đoạn code khác giữ nguyên)

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private List<CongViecNoiBoDto> _fullCongViecNoiBoList = new();
        private async void CongViecNoiBoDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CongViecNoiBoGrid.SelectedItem is not CongViecNoiBoDto selected) return;

            selected.DaHoanThanh = !selected.DaHoanThanh;
            selected.NgayGio = DateTime.Now;

            var api = new CongViecNoiBoApi();
            var result = await api.UpdateAsync(selected.Id, selected);

            if (!result.IsSuccess)
            {
                MessageBox.Show($"Lỗi: {result.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var updated = result.Data!;
            selected.DaHoanThanh = updated.DaHoanThanh;
            selected.NgayGio = updated.NgayGio;
            selected.LastModified = updated.LastModified;

            SearchCongViecTextBox.Text = ""; // ✅ Xoá lọc
            ReloadCongViecNoiBoUI();
            SearchCongViecTextBox.Focus();// ✅ Reload full danh sách
        }
        private void SearchCongViecNoiBoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyCongViecNoiBoFilter();
        }
        private void ApplyCongViecNoiBoFilter()
        {
            string keyword = SearchCongViecTextBox.Text.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                CongViecNoiBoGrid.ItemsSource = _fullCongViecNoiBoList;
            }
            else
            {
                var filtered = _fullCongViecNoiBoList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();

                CongViecNoiBoGrid.ItemsSource = filtered;
            }
        }
        private void ReloadCongViecNoiBoUI()
        {
            _fullCongViecNoiBoList = AppProviders.CongViecNoiBos.Items
                .Where(x => !x.IsDeleted)
                .OrderBy(x => x.DaHoanThanh)
                .ThenByDescending(x => x.LastModified)
                .ToList();

            ApplyCongViecNoiBoFilter();
        }





        private List<ChiTietHoaDonNoDto> _fullChiTietHoaDonNoList = new();
        private async void ChiTietHoaDonNoDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChiTietHoaDonNoGrid.SelectedItem is not ChiTietHoaDonNoDto selected) return;

            selected.NgayGio = DateTime.Now;

            var api = new ChiTietHoaDonNoApi();
            var result = await api.UpdateAsync(selected.Id, selected);

            if (!result.IsSuccess)
            {
                MessageBox.Show($"Lỗi: {result.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
                await AppProviders.ChiTietHoaDonNos.ReloadAsync();
        }
        private void SearchChiTietHoaDonNoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyChiTietHoaDonNoFilter();
        }
        private void ApplyChiTietHoaDonNoFilter()
        {
            string keyword = SearchChiTietHoaDonNoTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                ChiTietHoaDonNoGrid.ItemsSource = _fullChiTietHoaDonNoList;
                tongTien = _fullChiTietHoaDonNoList.Sum(x => x.SoTienNo);

            }
            else
            {
                var filtered = _fullChiTietHoaDonNoList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();

                ChiTietHoaDonNoGrid.ItemsSource = filtered;
                tongTien = filtered.Sum(x => x.SoTienNo);


            }
            // ✅ Tính tổng nợ
            TongTienNoTextBlock.Header = $"{tongTien:N0} đ";

        }
        private void ReloadChiTietHoaDonNoUI()
        {
            _fullChiTietHoaDonNoList = AppProviders.ChiTietHoaDonNos.Items
                .Where(x => !x.IsDeleted && x.SoTienConNo > 0)
                .OrderByDescending(x => x.LastModified)
                .ToList();
            ApplyChiTietHoaDonNoFilter();
            ThongKeMhanh();

        }





        private List<ChiTietHoaDonThanhToanDto> _fullChiTietHoaDonThanhToanList = new();
        private async void ChiTietHoaDonThanhToanDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChiTietHoaDonThanhToanGrid.SelectedItem is not ChiTietHoaDonThanhToanDto selected) return;

            selected.NgayGio = DateTime.Now;

            var api = new ChiTietHoaDonThanhToanApi();
            var result = await api.UpdateAsync(selected.Id, selected);

            if (!result.IsSuccess)
            {
                MessageBox.Show($"Lỗi: {result.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();
        }
        private void SearchChiTietHoaDonThanhToanTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyChiTietHoaDonThanhToanFilter();
        }
        private void ApplyChiTietHoaDonThanhToanFilter()
        {
            string keyword = SearchChiTietHoaDonThanhToanTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                ChiTietHoaDonThanhToanGrid.ItemsSource = _fullChiTietHoaDonThanhToanList;
                tongTien = _fullChiTietHoaDonThanhToanList.Sum(x => x.SoTien);
            }
            else
            {
                var filtered = _fullChiTietHoaDonThanhToanList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();
                ChiTietHoaDonThanhToanGrid.ItemsSource = filtered;
                tongTien = filtered.Sum(x => x.SoTien);
            }
            // ✅ Tính tổng nợ
            TongTienThanhToanTextBlock.Header = $"{tongTien:N0} đ";

        }
        private void ReloadChiTietHoaDonThanhToanUI()
        {
            _fullChiTietHoaDonThanhToanList = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => !x.IsDeleted)
                .Where(x => x.Ngay == now)
                .OrderByDescending(x => x.LastModified)
                .ToList();
            ApplyChiTietHoaDonThanhToanFilter();

            ThongKeMhanh();

        }






        private List<ChiTieuHangNgayDto> _fullChiTieuHangNgayList = new();
        private async void AddChiTieuHangNgayButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChiTieuHangNgayEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };
            if (window.ShowDialog() == true)
                await AppProviders.ChiTieuHangNgays.ReloadAsync();
        }
        private async void ChiTieuHangNgayDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChiTieuHangNgayDataGrid.SelectedItem is not ChiTieuHangNgayDto selected) return;
            var window = new ChiTieuHangNgayEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };
            if (window.ShowDialog() == true)
                await AppProviders.ChiTieuHangNgays.ReloadAsync();
        }
        private void SearchChiTieuHangNgayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyChiTieuHangNgayFilter();
        }
        private void ApplyChiTieuHangNgayFilter()
        {
            string keyword = SearchChiTieuHangNgayTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                ChiTieuHangNgayDataGrid.ItemsSource = _fullChiTieuHangNgayList;
                tongTien = _fullChiTieuHangNgayList
                    .Where(x => !x.BillThang)
                    .Sum(x => x.ThanhTien);
            }
            else
            {
                var filtered = _fullChiTieuHangNgayList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();

                ChiTieuHangNgayDataGrid.ItemsSource = filtered;
                tongTien = filtered
                    .Where(x => !x.BillThang)
                    .Sum(x => x.ThanhTien);
            }
            // ✅ Tính tổng nợ
            TongTienChiTieuTextBlock.Header = $"{tongTien:N0} đ";
        }
        private void ReloadChiTieuHangNgayUI()
        {
            _fullChiTieuHangNgayList = AppProviders.ChiTieuHangNgays.Items
                .Where(x => !x.IsDeleted && x.Ngay == now)
                .OrderByDescending(x => x.NgayGio)
                .ToList();
            ApplyChiTieuHangNgayFilter();

            ThongKeMhanh();
        }




        private void ThongKeMhanh()
        {
        }




        private List<HoaDonDto> _fullHoaDonList = new();
        private async void AddHoaDonButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new HoaDonEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };
            if (window.ShowDialog() == true)
                await AppProviders.HoaDons.ReloadAsync();
        }
        private async void HoaDonDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (HoaDonDataGrid.SelectedItem is not HoaDonDto selected) return;
            var window = new HoaDonEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };
            if (window.ShowDialog() == true)
                await AppProviders.HoaDons.ReloadAsync();
        }
        private void SearchHoaDonTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyHoaDonFilter();
        }
        private void ApplyHoaDonFilter()
        {
            string keyword = SearchHoaDonTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
            if (string.IsNullOrWhiteSpace(keyword))
            {
                HoaDonDataGrid.ItemsSource = _fullHoaDonList;
                tongTien = _fullHoaDonList.Sum(x => x.ThanhTien);

            }
            else
            {
                var filtered = _fullHoaDonList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();

                HoaDonDataGrid.ItemsSource = filtered;
                tongTien = filtered.Sum(x => x.ThanhTien);
            }
            // ✅ Tính tổng nợ
            TongTienHoaDonTextBlock.Header = $"{tongTien:N0} đ";

        }
        private void ReloadHoaDonUI()
        {
            _fullHoaDonList = AppProviders.HoaDons.Items
                .Where(x => !x.IsDeleted && x.Ngay == now)
                .OrderByDescending(x => x.NgayGio)
                .ToList();
            ApplyHoaDonFilter();

            ThongKeMhanh();

        }




        string oldConn =
"Server=192.168.1.85;Database=DennCoffee;user=sa;password=baothanh1991;TrustServerCertificate=True";
        string newConn =
"Server=.;Database=TraSuaAppDb;Trusted_Connection=True;TrustServerCertificate=True";
        private DateTime now;

        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var importer = new KhachHangImporter(oldConn, newConn);
            await importer.ImportAsync();

            var importer2 = new HoaDonImporter(oldConn, newConn);
            await importer2.ImportTodayAsync();
        }



    }
}
