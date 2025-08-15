using System.Net.Http.Json;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.HoaDonViews;
using TraSuaApp.WpfClient.Services;
using TraSuaApp.WpfClient.SettingsViews;

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

            _nhacNhoTimer = new DispatcherTimer();
            _nhacNhoTimer.Interval = TimeSpan.FromMinutes(15);
            _nhacNhoTimer.Tick += async (s, e) => await KiemTraCongViec();
            //_nhacNhoTimer.Start();
            _ = KiemTraCongViec();
        }

        private DispatcherTimer _nhacNhoTimer;
        private async Task KiemTraCongViec()
        {
            if (AppProviders.CongViecNoiBos == null) return;
            var today = DateTime.Today;

            var congViec = AppProviders.CongViecNoiBos.Items
                .Where(cv => !cv.DaHoanThanh)
                .OrderBy(x => x.NgayGio)
                .SingleOrDefault();
            if (congViec != null)
            {
                await TTSHelper.DownloadAndPlayGoogleTTSAsync(congViec.Ten);
            }

        }


        private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await ApiClient.GetAsync("/api/dashboard/homnay");
                var dashboard = await result.Content.ReadFromJsonAsync<DashboardDto>();
                if (dashboard == null) return;
                now = DateTime.Today.AddDays(0);

                BanNhieuGrid.ItemsSource = dashboard.TopSanPhams;

                while (AppProviders.CongViecNoiBos?.Items == null)
                {
                    await Task.Delay(100);
                }
                AppProviders.CongViecNoiBos.OnChanged += ReloadCongViecNoiBoUI;
                while (AppProviders.ChiTietHoaDonNos?.Items == null)
                {
                    await Task.Delay(100);
                }
                AppProviders.ChiTietHoaDonNos.OnChanged += ReloadChiTietHoaDonNoUI;

                while (AppProviders.ChiTietHoaDonThanhToans?.Items == null)
                {
                    await Task.Delay(100);
                }
                AppProviders.ChiTietHoaDonThanhToans.OnChanged += ReloadChiTietHoaDonThanhToanUI;

                while (AppProviders.ChiTieuHangNgays?.Items == null)
                {
                    await Task.Delay(100);
                }
                AppProviders.ChiTieuHangNgays.OnChanged += ReloadChiTieuHangNgayUI;

                while (AppProviders.HoaDons?.Items == null)
                {
                    await Task.Delay(100);
                }
                AppProviders.HoaDons.OnChanged += ReloadHoaDonUI;



            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dashboard: " + ex.Message);
            }
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

                await Task.Delay(100);

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
        }







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

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

                this.WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }





        private List<CongViecNoiBoDto> _fullCongViecNoiBoList = new();
        private async void AddCongViecNoiBoButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new CongViecNoiBoEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
            };
            if (window.ShowDialog() == true)
                await AppProviders.CongViecNoiBos.ReloadAsync();
        }
        private async void XoaCongViecNoiBoButton_Click(object sender, RoutedEventArgs e)
        {
            if (CongViecNoiBoDataGrid.SelectedItem is not CongViecNoiBoDto selected)
                return;
            var confirm = MessageBox.Show(
               $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
               "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/CongViecNoiBo/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<CongViecNoiBoDto>>();

                if (result?.IsSuccess == true)
                {
                    AppProviders.CongViecNoiBos.Remove(selected.Id);
                }
                else
                {
                    _errorHandler.Handle(new Exception(result?.Message ?? "Không thể xoá."), "Delete");
                }
            }
            catch (Exception ex)
            {
                // Khối catch này vẫn hữu ích để bắt các lỗi mạng hoặc lỗi không xác định
                _errorHandler.Handle(ex, "Delete");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }
        private async void CongViecNoiBoDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CongViecNoiBoDataGrid.SelectedItem is not CongViecNoiBoDto selected) return;

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

            SearchCongViecNoiBoTextBox.Text = "";
            ReloadCongViecNoiBoUI();
            SearchCongViecNoiBoTextBox.Focus();
        }
        private void SearchCongViecNoiBoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyCongViecNoiBoFilter();
        }
        private void ApplyCongViecNoiBoFilter()
        {
            string keyword = SearchCongViecNoiBoTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
            List<CongViecNoiBoDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullCongViecNoiBoList;
            }
            else
            {
                sourceList = _fullCongViecNoiBoList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();
            }

            // Gán số thứ tự
            int stt = 1;
            foreach (var item in sourceList)
            {
                item.Stt = stt++;
            }

            CongViecNoiBoDataGrid.ItemsSource = sourceList;
            // tongTien = sourceList.Sum(x => x.ThanhTien);

            //TongTienCongViecNoiBoTextBlock.Header = $"{tongTien:N0} đ";

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
            if (ChiTietHoaDonNoDataGrid.SelectedItem is not ChiTietHoaDonNoDto selected) return;

            // Tạo model thanh toán mới từ chi tiết nợ
            var dto = new ChiTietHoaDonThanhToanDto
            {
                ChiTietHoaDonNoId = selected.Id,
                HoaDonId = selected.HoaDonId,
                KhachHangId = selected.KhachHangId,
                Ten = $"{selected.Ten}",
                LoaiThanhToan = "Trả nợ",
                SoTien = selected.SoTienNo, // mặc định thanh toán toàn bộ số nợ
                GhiChu = $"{selected.Ten} thanh toán nợ: {selected.MaHoaDon}"
            };

            var window = new ChiTietHoaDonThanhToanEdit(dto)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
            };

            if (window.ShowDialog() == true)
            {
                await AppProviders.ChiTietHoaDonNos.ReloadAsync();
            }
        }
        private void SearchChiTietHoaDonNoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyChiTietHoaDonNoFilter();
        }
        private void ApplyChiTietHoaDonNoFilter()
        {
            string keyword = SearchChiTietHoaDonNoTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
            List<ChiTietHoaDonNoDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullChiTietHoaDonNoList;
            }
            else
            {
                sourceList = _fullChiTietHoaDonNoList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();
            }

            // Gán số thứ tự
            int stt = 1;
            foreach (var item in sourceList)
            {
                item.Stt = stt++;
            }

            ChiTietHoaDonNoDataGrid.ItemsSource = sourceList;
            tongTien = sourceList.Sum(x => x.SoTienConNo);

            TongTienChiTietHoaDonNoTextBlock.Header = $"{tongTien:N0} đ";



        }
        private void ReloadChiTietHoaDonNoUI()
        {
            _fullChiTietHoaDonNoList = AppProviders.ChiTietHoaDonNos.Items
                .Where(x => !x.IsDeleted && x.SoTienConNo > 0)
                // .Where(x => !x.IsDeleted && x.Ngay == now)
                .OrderByDescending(x => x.LastModified)
                .ToList();
            ApplyChiTietHoaDonNoFilter();
            UpdateDashboardSummary();
        }






        private List<ChiTietHoaDonThanhToanDto> _fullChiTietHoaDonThanhToanList = new();
        private async void ChiTietHoaDonThanhToanDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ChiTietHoaDonThanhToanDataGrid.SelectedItem is not ChiTietHoaDonThanhToanDto selected) return;
            var window = new ChiTietHoaDonThanhToanEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
            };
            if (window.ShowDialog() == true)
                await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();
        }
        private async void XoaChiTietHoaDonThanhToanButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietHoaDonThanhToanDataGrid.SelectedItem is not ChiTietHoaDonThanhToanDto selected)
                return;
            var confirm = MessageBox.Show(
               $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
               "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/ChiTietHoaDonThanhToan/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<ChiTietHoaDonThanhToanDto>>();

                if (result?.IsSuccess == true)
                {
                    AppProviders.ChiTietHoaDonThanhToans.Remove(selected.Id);
                }
                else
                {
                    _errorHandler.Handle(new Exception(result?.Message ?? "Không thể xoá."), "Delete");
                }
            }
            catch (Exception ex)
            {
                // Khối catch này vẫn hữu ích để bắt các lỗi mạng hoặc lỗi không xác định
                _errorHandler.Handle(ex, "Delete");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }
        private void SearchChiTietHoaDonThanhToanTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyChiTietHoaDonThanhToanFilter();
        }
        private void ApplyChiTietHoaDonThanhToanFilter()
        {

            string keyword = SearchChiTietHoaDonThanhToanTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
            List<ChiTietHoaDonThanhToanDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullChiTietHoaDonThanhToanList;
            }
            else
            {
                sourceList = _fullChiTietHoaDonThanhToanList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();
            }

            // Gán số thứ tự
            int stt = 1;
            foreach (var item in sourceList)
            {
                item.Stt = stt++;
            }

            ChiTietHoaDonThanhToanDataGrid.ItemsSource = sourceList;
            tongTien = sourceList.Sum(x => x.SoTien);

            TongTienThanhToanTextBlock.Header = $"{tongTien:N0} đ";
        }
        private void ReloadChiTietHoaDonThanhToanUI()
        {
            _fullChiTietHoaDonThanhToanList = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => !x.IsDeleted)
                .Where(x => x.Ngay == now)
                .OrderByDescending(x => x.NgayGio)
                .ToList();
            ApplyChiTietHoaDonThanhToanFilter();
            UpdateDashboardSummary();
        }






        private List<ChiTieuHangNgayDto> _fullChiTieuHangNgayList = new();
        private async void AddChiTieuHangNgayButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new ChiTieuHangNgayEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
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
                Height = this.ActualHeight,
                Owner = this
            };
            if (window.ShowDialog() == true)
                await AppProviders.ChiTieuHangNgays.ReloadAsync();
        }
        private async void XoaChiTieuHangNgayButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTieuHangNgayDataGrid.SelectedItem is not ChiTieuHangNgayDto selected)
                return;
            var confirm = MessageBox.Show(
               $"Bạn có chắc chắn muốn xoá '{selected.Ten}'?",
               "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/ChiTieuHangNgay/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<ChiTieuHangNgayDto>>();

                if (result?.IsSuccess == true)
                {
                    AppProviders.ChiTieuHangNgays.Remove(selected.Id);
                }
                else
                {
                    _errorHandler.Handle(new Exception(result?.Message ?? "Không thể xoá."), "Delete");
                }
            }
            catch (Exception ex)
            {
                // Khối catch này vẫn hữu ích để bắt các lỗi mạng hoặc lỗi không xác định
                _errorHandler.Handle(ex, "Delete");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private readonly WpfErrorHandler _errorHandler = new();
        private void SearchChiTieuHangNgayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyChiTieuHangNgayFilter();
        }
        private void ApplyChiTieuHangNgayFilter()
        {
            string keyword = SearchChiTieuHangNgayTextBox.Text.Trim().ToLower();
            decimal tongTien = 0;
            List<ChiTieuHangNgayDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullChiTieuHangNgayList;
            }
            else
            {
                sourceList = _fullChiTieuHangNgayList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();
            }

            // Gán số thứ tự
            int stt = 1;
            foreach (var item in sourceList)
            {
                item.Stt = stt++;
            }

            ChiTieuHangNgayDataGrid.ItemsSource = sourceList;
            tongTien = sourceList.Sum(x => x.ThanhTien);

            TongTienChiTieuHangNgayTextBlock.Header = $"{tongTien:N0} đ";

        }
        private void ReloadChiTieuHangNgayUI()
        {
            _fullChiTieuHangNgayList = AppProviders.ChiTieuHangNgays.Items
                .Where(x => !x.IsDeleted && x.Ngay == now)
                .OrderBy(x => x.BillThang)
                .ThenByDescending(x => x.NgayGio)
                .ToList();
            ApplyChiTieuHangNgayFilter();
            UpdateDashboardSummary();
        }




        private void ThongKeHomNay()
        {
            var today = now;
            var hoaDons = AppProviders.HoaDons.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .ToList();
            LoadDoanhThuDynamic(hoaDons);
            LoadChuaThuDynamic(hoaDons);
            LoadCongNoDynamic(hoaDons);

            var thanhToans = AppProviders.ChiTietHoaDonThanhToans.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .ToList();
            LoadDaThuDynamic(thanhToans);
            LoadTraNoTienDynamic(thanhToans);
            LoadTraNoBankDynamic(thanhToans);

            var chiTieu = AppProviders.ChiTieuHangNgays.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .ToList();
            LoadChiTieuDynamic(chiTieu);
            LoadMangVeDynamic(chiTieu, thanhToans, hoaDons);


            var congNo = AppProviders.ChiTietHoaDonNos.Items
                .Where(x => !x.IsDeleted && x.Ngay == today)
                .ToList();

        }
        private void LoadDoanhThuDynamic(List<HoaDonDto> hoaDons)
        {
            DoanhThuStackPanel.Children.Clear();

            var groups = hoaDons
                .GroupBy(x => x.PhanLoai)
                .Select(g => new
                {
                    Loai = g.Key,
                    TongTien = g.Sum(x => x.ThanhTien)
                })
                .OrderBy(g => g.Loai)
                .OrderByDescending(g => g.TongTien)
                .ToList();

            // Tính tổng tất cả
            decimal tongTatCa = groups.Sum(g => g.TongTien);

            // Dòng tổng trên cùng (chữ to, đậm)
            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Doanh thu\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            DoanhThuStackPanel.Children.Add(totalTextBlock);

            // Các dòng chi tiết
            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock { Text = $"{group.Loai}:" };
                var tienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                DoanhThuStackPanel.Children.Add(grid);
            }
        }
        private void LoadDaThuDynamic(List<ChiTietHoaDonThanhToanDto> chiTietThanhToans)
        {
            DaThuStackPanel.Children.Clear();

            var groups = chiTietThanhToans
                .Where(x => x.LoaiThanhToan.ToLower() == "trong ngày")
                .GroupBy(x => x.TenPhuongThucThanhToan) // Ví dụ: "Tiền mặt", "Chuyển khoản", ...
                .Select(g => new
                {
                    Loai = g.Key,
                    TongTien = g.Sum(x => x.SoTien)
                })
                .OrderByDescending(g => g.TongTien)
                .ToList();

            // Tính tổng tất cả
            decimal tongTatCa = groups.Sum(g => g.TongTien);

            // Dòng tổng trên cùng (chữ to, đậm, cn giữa)
            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Đã thu\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            DaThuStackPanel.Children.Add(totalTextBlock);

            // Các dòng chi tiết
            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock
                {
                    Text = $"{group.Loai}:",
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var tienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                DaThuStackPanel.Children.Add(grid);
            }
        }
        private void LoadChuaThuDynamic(List<HoaDonDto> hoaDons)
        {
            ChuaThuStackPanel.Children.Clear();
            // Lọc trước các hóa đơn còn nợ
            var hoaDonChuaThu = hoaDons
                .Where(x => x.ConLai > 0 &&
                x.TrangThai == "Chưa Thu")
                .OrderByDescending(g => g.TongTien)
                .ToList();


            // Tính tổng tất cả
            decimal tongTatCa = hoaDonChuaThu.Sum(g => g.ConLai);

            // Dòng tổng trên cùng (chữ to, đậm)
            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Chưa thu\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            ChuaThuStackPanel.Children.Add(totalTextBlock);

            // Các dòng chi tiết
            foreach (var hd in hoaDonChuaThu)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock
                {
                    Text = $"{hd.Ten}:",
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var tienText = new TextBlock
                {
                    Text = $"{hd.ConLai:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                ChuaThuStackPanel.Children.Add(grid);
            }
        }
        private void LoadCongNoDynamic(List<HoaDonDto> hoaDons)
        {
            CongNoStackPanel.Children.Clear();

            // Lọc trước các hóa đơn còn nợ hôm nay
            var hoaDonNo = hoaDons
                .Where(x => x.ConLai > 0
                    && x.TrangThai == "Ghi Nợ"
                    && x.Ngay == now)
                .ToList();

            // Gom nhóm theo KhachHangId
            var groups = hoaDonNo
                .GroupBy(x => new { x.KhachHangId, x.Ten })
                .Select(g => new
                {
                    KhachHangId = g.Key.KhachHangId,
                    TenKhachHang = g.Key.Ten,
                    TongTien = g.Sum(x => x.ConLai)
                })
                .OrderByDescending(g => g.TongTien)
                .ToList();

            // Tính tổng tất cả
            decimal tongTatCa = groups.Sum(g => g.TongTien);

            // Dòng tổng trên cùng (chữ to, đậm)
            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Ghi nợ\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            CongNoStackPanel.Children.Add(totalTextBlock);

            // Các dòng chi tiết
            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock
                {
                    Text = $"{group.TenKhachHang}:",
                    VerticalAlignment = VerticalAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                var tienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                CongNoStackPanel.Children.Add(grid);
            }
        }
        private void LoadChiTieuDynamic(List<ChiTieuHangNgayDto> chiTieuList)
        {
            ChiTieuStackPanel.Children.Clear();

            // Gom nhóm theo NguyenLieuId
            var groups = chiTieuList
                .Where(x => x.BillThang == false)
                .GroupBy(x => new { x.NguyenLieuId, x.Ten })
                .Select(g => new
                {
                    NguyenLieuId = g.Key.NguyenLieuId,
                    TenNguyenLieu = g.Key.Ten,
                    TongTien = g.Sum(x => x.ThanhTien)
                })
                .OrderByDescending(g => g.TongTien)
                .ToList();

            // Tính tổng chi tiêu hôm nay
            decimal tongTatCa = groups.Sum(g => g.TongTien);

            // Hiển thị dòng tổng trên cùng
            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Chi tiêu\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            ChiTieuStackPanel.Children.Add(totalTextBlock);

            // Hiển thị chi tiết từng nguyên liệu
            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var tenNguyenLieuText = new TextBlock
                {
                    Text = $"{group.TenNguyenLieu}:",
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                var soTienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right
                };

                Grid.SetColumn(tenNguyenLieuText, 0);
                Grid.SetColumn(soTienText, 1);

                grid.Children.Add(tenNguyenLieuText);
                grid.Children.Add(soTienText);

                ChiTieuStackPanel.Children.Add(grid);
            }
        }
        private void LoadMangVeDynamic(List<ChiTieuHangNgayDto> chiTieuList, List<ChiTietHoaDonThanhToanDto> thanhToanList, List<HoaDonDto> hoaDonList)
        {
            MangVeStackPanel.Children.Clear();

            // Tính tổng tất cả
            decimal tongTienMat = thanhToanList
                .Where(x => x.LoaiThanhToan.ToLower() == "trong ngày")
              .Where(x => x.TenPhuongThucThanhToan?.ToLower() == "tiền mặt")
              .Sum(x => x.SoTien);
            decimal tongChiTieu = chiTieuList
                .Where(x => !x.BillThang)
                .Sum(x => x.ThanhTien);
            decimal tongChuaThu = hoaDonList
               .Where(x => x.ConLai > 0 && x.TrangThai.ToLower() == "chưa thu")
               .Sum(g => g.ConLai);
            decimal tongTatCa = tongTienMat - tongChiTieu + tongChuaThu;

            decimal tongTraNo = thanhToanList
           .Where(x =>
           x.LoaiThanhToan?.ToLower().Contains("trả nợ") == true
           &&
           x.TenPhuongThucThanhToan?.ToLower().Contains("tiền mặt") == true
           )
           .Sum(x => x.SoTien);
            // Hiển thị số tiền mặt mang về (tongTatCa) ở trên cùng
            var mangVeTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            mangVeTextBlock.Inlines.Add(new Run("Mang về\n"));
            mangVeTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            MangVeStackPanel.Children.Add(mangVeTextBlock);

            // Hiển thị tổng tiền mặt
            var tongTienMatGrid = new Grid();
            tongTienMatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tongTienMatGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tongTienMatLabel = new TextBlock
            {
                Text = "Tiền mặt:",
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tongTienMatValue = new TextBlock
            {
                Text = $"{tongTienMat:N0} đ",
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };

            Grid.SetColumn(tongTienMatLabel, 0);
            Grid.SetColumn(tongTienMatValue, 1);
            tongTienMatGrid.Children.Add(tongTienMatLabel);
            tongTienMatGrid.Children.Add(tongTienMatValue);

            MangVeStackPanel.Children.Add(tongTienMatGrid);

            // Hiển thị tổng chi tiêu
            var tongChuaThuGrid = new Grid();
            tongChuaThuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tongChuaThuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tongChuaThuLabel = new TextBlock
            {
                Text = "Chưa thu:",
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tongChuaThuValue = new TextBlock
            {
                Text = $"{tongChuaThu:N0} đ",
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };

            Grid.SetColumn(tongChuaThuLabel, 0);
            Grid.SetColumn(tongChuaThuValue, 1);
            tongChuaThuGrid.Children.Add(tongChuaThuLabel);
            tongChuaThuGrid.Children.Add(tongChuaThuValue);

            MangVeStackPanel.Children.Add(tongChuaThuGrid);




            // Hiển thị tổng chi tiêu
            var tongChiTieuGrid = new Grid();
            tongChiTieuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tongChiTieuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tongChiTieuLabel = new TextBlock
            {
                Text = "Chi tiêu:",
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tongChiTieuValue = new TextBlock
            {
                Text = $"{-tongChiTieu:N0} đ",
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };

            Grid.SetColumn(tongChiTieuLabel, 0);
            Grid.SetColumn(tongChiTieuValue, 1);
            tongChiTieuGrid.Children.Add(tongChiTieuLabel);
            tongChiTieuGrid.Children.Add(tongChiTieuValue);

            MangVeStackPanel.Children.Add(tongChiTieuGrid);

            var tongThucTeGrid = new Grid();
            tongThucTeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tongThucTeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tongThucTeLabel = new TextBlock
            {
                Text = "\nKiểm tiền:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,

                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tongThucTeValue = new TextBlock
            {
                Text = $"\n{tongTatCa - tongChuaThu:N0} đ",
                FontWeight = FontWeights.Bold,
                FontSize = 14,

                TextAlignment = TextAlignment.Right
            };

            Grid.SetColumn(tongThucTeLabel, 0);
            Grid.SetColumn(tongThucTeValue, 1);
            tongThucTeGrid.Children.Add(tongThucTeLabel);
            tongThucTeGrid.Children.Add(tongThucTeValue);

            MangVeStackPanel.Children.Add(tongThucTeGrid);

            // Hiển thị tổng tra no
            var tongTraNoGrid = new Grid();
            tongTraNoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tongTraNoGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var tongTraNoLabel = new TextBlock
            {
                Text = "Trả nợ tiền:",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var tongTraNoValue = new TextBlock
            {
                Text = $"{tongTraNo:N0} đ",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Right
            };

            Grid.SetColumn(tongTraNoLabel, 0);
            Grid.SetColumn(tongTraNoValue, 1);
            tongTraNoGrid.Children.Add(tongTraNoLabel);
            tongTraNoGrid.Children.Add(tongTraNoValue);

            MangVeStackPanel.Children.Add(tongTraNoGrid);

            var copyButton = new Button
            {
                Content = "Copy -> Ctrl v",
                Foreground = Brushes.WhiteSmoke,
                Style = (Style)FindResource("ThemButtonStyle")
            };

            copyButton.Click += (s, e) =>
            {
                copyButton.Visibility = Visibility.Collapsed;

                // Lưu kích thước thật
                var size = new System.Windows.Size(MangVeBorder.ActualWidth, MangVeBorder.ActualHeight);

                // RenderTargetBitmap với DPI cao để ảnh mịn
                double dpi = 192; // gấp đôi 96 để mịn hơn
                var rtb = new RenderTargetBitmap(
                    (int)(size.Width * dpi / 96),
                    (int)(size.Height * dpi / 96),
                    dpi, dpi,
                    PixelFormats.Pbgra32);

                // Vẽ nền để tránh bị trong suốt/mất màu
                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.DrawRectangle(new VisualBrush(MangVeBorder), null, new Rect(size));
                }
                rtb.Render(dv);

                // Copy vào clipboard
                Clipboard.SetImage(rtb);

                copyButton.Visibility = Visibility.Visible;
            };
            MangVeStackPanel.Children.Add(copyButton);

        }
        private void LoadTraNoBankDynamic(List<ChiTietHoaDonThanhToanDto> thanhToans)
        {
            TraNoBankStackPanel.Children.Clear();

            // Nhóm theo KhachHangId
            var groups = thanhToans
                .Where(x => x.LoaiThanhToan.ToLower() == "trả nợ")
                .Where(x => x.TenPhuongThucThanhToan?.ToLower() != "tiền mặt")
                .GroupBy(x => x.KhachHangId)
                .Select(g => new
                {
                    KhachHangId = g.Key,
                    Ten = g.FirstOrDefault()?.Ten ?? "Khách hàng lạ",
                    TongTien = g.Sum(x => x.SoTien)
                })
                .OrderByDescending(g => g.TongTien)
                .ToList();

            // Tổng tất cả
            decimal tongTatCa = groups.Sum(g => g.TongTien);

            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Trả nợ bank\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            TraNoBankStackPanel.Children.Add(totalTextBlock);

            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock
                {
                    Text = $"{group.Ten}:",
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var tienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                TraNoBankStackPanel.Children.Add(grid);
            }
        }
        private void LoadTraNoTienDynamic(List<ChiTietHoaDonThanhToanDto> thanhToans)
        {
            TraNoTienStackPanel.Children.Clear();

            var groups = thanhToans
                .Where(x => x.LoaiThanhToan.ToLower() == "trả nợ")
                .Where(x => x.TenPhuongThucThanhToan?.ToLower() == "tiền mặt")
                .GroupBy(x => x.KhachHangId)
                .Select(g => new
                {
                    KhachHangId = g.Key,
                    Ten = g.FirstOrDefault()?.Ten ?? "Khách hàng lạ",
                    TongTien = g.Sum(x => x.SoTien)
                })
                .OrderByDescending(g => g.TongTien)
                .ToList();

            decimal tongTatCa = groups.Sum(g => g.TongTien);

            var totalTextBlock = new TextBlock
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
            };
            totalTextBlock.Inlines.Add(new Run("Trả nợ tiền\n"));
            totalTextBlock.Inlines.Add(new Run($"{tongTatCa:N0} đ") { FontWeight = FontWeights.Bold });
            TraNoTienStackPanel.Children.Add(totalTextBlock);

            foreach (var group in groups)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var loaiText = new TextBlock
                {
                    Text = $"{group.Ten}:",
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    TextWrapping = TextWrapping.NoWrap,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var tienText = new TextBlock
                {
                    Text = $"{group.TongTien:N0} đ",
                    FontWeight = FontWeights.Bold,
                    TextAlignment = TextAlignment.Right
                };

                Grid.SetColumn(loaiText, 0);
                Grid.SetColumn(tienText, 1);

                grid.Children.Add(loaiText);
                grid.Children.Add(tienText);

                TraNoTienStackPanel.Children.Add(grid);
            }
        }

        private bool _isUpdatingDashboard = false;
        private void UpdateDashboardSummary()
        {
            // Nếu có một quá trình cập nhật khác đang chạy, thì thoát
            if (_isUpdatingDashboard) return;
            try
            {
                _isUpdatingDashboard = true;
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    // Đây là nơi bạn đặt toàn bộ logic của hàm ThongKeHomNay()
                    ThongKeHomNay();
                    _isUpdatingDashboard = false;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
            catch
            {
                _isUpdatingDashboard = false;
            }
        }






        private List<HoaDonDto> _fullHoaDonList = new();
        private async void AddHoaDonButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new HoaDonEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this,
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
                Height = this.ActualHeight,
                Owner = this
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
            List<HoaDonDto> sourceList;

            if (string.IsNullOrWhiteSpace(keyword))
            {
                sourceList = _fullHoaDonList;
            }
            else
            {
                sourceList = _fullHoaDonList
                    .Where(x => x.TimKiem.ToLower().Contains(keyword))
                    .ToList();
            }

            // Gán số thứ tự
            int stt = 1;
            foreach (var item in sourceList)
            {
                item.Stt = stt++;
            }

            HoaDonDataGrid.ItemsSource = sourceList;
            tongTien = sourceList.Sum(x => x.ThanhTien);

            TongTienHoaDonTextBlock.Header = $"{tongTien:N0} đ";

        }
        private void ReloadHoaDonUI()
        {
            _fullHoaDonList = AppProviders.HoaDons.Items
                .Where(x => !x.IsDeleted && x.Ngay == now)
                .OrderByDescending(x => x.NgayGio)
                .ToList();
            ApplyHoaDonFilter();
            UpdateDashboardSummary();
        }





        string oldConn = "Server=192.168.1.85;Database=DennCoffee;user=sa;password=baothanh1991;TrustServerCertificate=True";
        string newConn = "Server=.;Database=TraSuaAppDb;Trusted_Connection=True;TrustServerCertificate=True";
        private DateTime now;
        private async void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var importer = new KhachHangImporter(oldConn, newConn);
            await importer.ImportAsync();

            var importer2 = new HoaDonImporter(oldConn, newConn);
            await importer2.ImportTodayAsync();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl tabControl)
            {
                var selectedTab = tabControl.SelectedItem as TabItem;
                if (selectedTab != null)
                {
                    if (selectedTab.Tag.ToString() == "HoaDon")
                    {
                        //if (AppProviders.HoaDons == null) return;
                        await AppProviders.HoaDons.ReloadAsync();
                        ReloadHoaDonUI();
                    }
                    else if (selectedTab.Tag.ToString() == "ChiTieuHangNgay")
                    {
                        await AppProviders.ChiTieuHangNgays.ReloadAsync();
                        ReloadChiTieuHangNgayUI();
                    }
                    else if (selectedTab.Tag.ToString() == "ChiTietHoaDonNo")
                    {
                        await AppProviders.ChiTietHoaDonNos.ReloadAsync();
                        ReloadChiTietHoaDonNoUI();
                    }
                    else if (selectedTab.Tag.ToString() == "CongViecNoiBo")
                    {
                        await AppProviders.CongViecNoiBos.ReloadAsync();
                        ReloadCongViecNoiBoUI();
                    }
                    else if (selectedTab.Tag.ToString() == "ChiTietHoaDonThanhToan")
                    {
                        await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();
                        ReloadChiTietHoaDonThanhToanUI();
                    }
                }
            }



        }
    }
}
