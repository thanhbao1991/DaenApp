using System.IO;
using System.Windows;
using Microsoft.Win32;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Tools
{
    public partial class ImportSanPhamWindow : Window
    {
        private readonly UIExceptionHelper _errorHandler = new WpfErrorHandler();

        public ImportSanPhamWindow()
        {
            InitializeComponent();
            Loaded += ImportSanPhamWindow_Loaded;
        }

        private async void ImportSanPhamWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Chọn file dữ liệu sản phẩm cũ",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                Close();
                return;
            }

            int count = 0;
            try
            {
                var lines = File.ReadAllLines(dialog.FileName);
                foreach (var line in lines)
                {
                    var parts = line.Split('\t');
                    if (parts.Length < 3) continue;

                    var OldId = int.Parse(parts[0]);
                    var ten = parts[1].Trim();
                    if (!decimal.TryParse(parts[2], out var donGia)) continue;

                    var ngungBan = parts.Length > 3 && parts[3] == "1" ? true : false;
                    var tichDiem = parts.Length > 4 && parts[4] == "1" ? true : false;
                    var vietTat = parts.Length > 5 ? parts[5].Trim() : null;
                    var dinhLuong = parts.Length > 6 ? parts[6].Trim() : null;

                    var sanPham = new SanPhamDto
                    {
                        Ten = ten,
                        OldId = OldId,
                        NgungBan = ngungBan,
                        TichDiem = tichDiem,
                        VietTat = vietTat,
                        DinhLuong = dinhLuong,
                        BienThe = new List<SanPhamBienTheDto>()
                        {
                            new SanPhamBienTheDto
                            {
                                TenBienThe = "Size Chuẩn",
                                GiaBan = donGia
                            },
                            new SanPhamBienTheDto
                            {
                                TenBienThe = "Size Lớn",
                                GiaBan = donGia + 5000
                            }
                        }
                    };

                    var response = await ApiClient.PostAsync("/api/sanpham", sanPham);
                    if (response.IsSuccessStatusCode)
                        count++;
                    else
                    {
                        var msg = await response.Content.ReadAsStringAsync();
                        _errorHandler.Handle(new Exception($"Dòng {OldId} - {ten}: {msg}"), "ImportSanPham");
                    }
                }

                MessageBox.Show($"Đã nhập thành công {count} sản phẩm.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "ImportSanPhamWindow_Loaded");
            }

            Close(); // Đóng sau khi hoàn tất
        }
    }
}
