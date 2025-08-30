using System.Diagnostics;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Controls;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.HoaDonViews
{
    public partial class HoaDonEdit : Window
    {
        public HoaDonDto Model { get; set; } = new();
        private readonly IHoaDonApi _api;
        string _friendlyName = TuDien._tableFriendlyNames["HoaDon"];

        private List<SanPhamDto> _sanPhamList = new();
        private List<SanPhamBienTheDto> _bienTheList = new();
        private List<ToppingDto> _toppingList = new();
        private List<VoucherDto> _voucherList = new();
        private List<KhachHangDto> _khachHangsList = new();
        private readonly string[] _banList = new[]
{
    "2", "3", "4", "5", "6",
    "7", "8", "9", "10", "13",
    "Sân 1", "Sân 2"
};



        public HoaDonEdit(HoaDonDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _sanPhamList = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();
            _bienTheList = _sanPhamList.SelectMany(x => x.BienThe).ToList();
            _toppingList = AppProviders.Toppings.Items.ToList();
            _voucherList = AppProviders.Vouchers.Items.ToList();
            _khachHangsList = AppProviders.KhachHangs.Items.ToList();

            SanPhamSearchBox.SanPhamSelected += kh =>
            {
                ResetSanPhamInputs();

                if (SanPhamSearchBox.SelectedSanPham is not SanPhamDto sp) return;

                // 🟟 Load danh sách biến thể theo sản phẩm
                var bienThes = _bienTheList.Where(x => x.SanPhamId == sp.Id).ToList();
                BienTheComboBox.ItemsSource = bienThes;

                // 🟟 Tự động chọn biến thể mặc định (rẻ nhất)
                var selectedBienThe = bienThes.OrderBy(x => x.GiaBan).FirstOrDefault();
                if (selectedBienThe != null)
                {
                    _isAddingFromSearch = true;
                    BienTheComboBox.SelectedValue = selectedBienThe.Id;
                    SoLuongTextBox.Text = "1";
                    CapNhatChiTietSanPham(1, true);
                    _isAddingFromSearch = false;

                    var addedItem = Model.ChiTietHoaDons.FirstOrDefault(x => x.SanPhamIdBienThe == selectedBienThe.Id);
                    if (addedItem != null)
                    {
                        ChiTietListBox.SelectedItem = addedItem;
                        ChiTietListBox.ScrollIntoView(addedItem);
                    }
                }

                LoadToppingPanel(sp.NhomSanPhamId);
            };



            KhachHangSearchBox.KhachHangList = _khachHangsList;
            KhachHangSearchBox.KhachHangSelected += async kh =>
            {
                Model.KhachHangId = kh.Id;
                Model.TenKhachHangText = kh.Ten;
                var diaChiList = kh.Addresses?.ToList() ?? new();
                DiaChiComboBox.ItemsSource = diaChiList;
                DiaChiComboBox.SelectedItem = diaChiList.FirstOrDefault(x => x.IsDefault) ?? diaChiList.LastOrDefault();
                var sdtList = kh.Phones?.ToList() ?? new();
                DienThoaiComboBox.ItemsSource = sdtList;
                DienThoaiComboBox.SelectedItem = sdtList.FirstOrDefault(x => x.IsDefault) ?? sdtList.LastOrDefault();


                try
                {
                    var response = await ApiClient.GetAsync($"/api/Dashboard/thongtin-khachhang/{kh.Id}");
                    var info = await response.Content.ReadFromJsonAsync<KhachHangFavoriteDto>();

                    if (info != null)
                    {
                        // Gán vào Model
                        Model.DiemThangNay = info.DiemThangNay;
                        Model.DiemThangTruoc = info.DiemThangTruoc;
                        Model.TongNoKhachHang = info.TongNo;
                        Model.DuocNhanVoucher = info.DuocNhanVoucher;
                        Model.DaNhanVoucher = info.DaNhanVoucher;

                        // Hiển thị điểm
                        CongNoTextBlock.Text = info.TongNo.ToString("N0");
                        DiemThangNayTextBlock.Text = StarHelper.GetStarText(Model.DiemThangNay);
                        DiemThangTruocTextBlock.Text = StarHelper.GetStarText(Model.DiemThangTruoc);

                        // 🟟 Kiem tra dieu kien de nhap nhay "Diem thang truoc"
                        if (info.DuocNhanVoucher && !info.DaNhanVoucher)
                        {
                            int saoDayTruoc = LoyaltyHelper.TinhSoSaoDay(Model.DiemThangTruoc);
                            int giaTriVoucher = LoyaltyHelper.TinhGiaTriVoucher(Model.DiemThangTruoc);

                            if (saoDayTruoc > 0 && giaTriVoucher > 0)
                            {
                                var blink = (Storyboard)FindResource("BlinkAnimation");
                                Storyboard.SetTarget(blink, DiemThangTruocGroupBox);
                                blink.Begin();
                            }
                        }

                        // Load top chi tiết
                        Model.ChiTietHoaDons.Clear();
                        foreach (var ct in info.TopChiTiets)
                        {
                            Model.ChiTietHoaDons.Add(new ChiTietHoaDonDto
                            {
                                Id = Guid.NewGuid(),
                                SanPhamIdBienThe = ct.SanPhamIdBienThe,
                                TenSanPham = ct.TenSanPham,
                                TenBienThe = ct.TenBienThe,
                                DonGia = ct.DonGia,
                                SoLuong = ct.SoLuong,
                                BienTheList = _bienTheList.Where(b => b.SanPhamId == ct.SanPhamIdBienThe).ToList(),
                                ToppingDtos = new List<ToppingDto>()
                            });
                        }

                        ChiTietListBox.ItemsSource = null;
                        ChiTietListBox.ItemsSource = Model.ChiTietHoaDons;
                        CapNhatTongTien();
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi tải món hay order nhất: " + ex.Message);
                }


                SanPhamSearchBox.SearchTextBox.Focus();



            };
            KhachHangSearchBox.KhachHangCleared += () =>
            {
                Model.KhachHangId = null;
                DiaChiComboBox.ItemsSource = null;
                DienThoaiComboBox.ItemsSource = null;
                CongNoTextBlock.Text = null;
                DiemThangNayTextBlock.Text = null;
                DiemThangTruocTextBlock.Text = null;

            };

            SanPhamSearchBox.SanPhamList = _sanPhamList;
            SanPhamSearchBox.SanPhamCleared += () => ResetSanPhamInputs();

            VoucherComboBox.ItemsSource = _voucherList;
            TenBanComboBox.ItemsSource = _banList;

            _api = new HoaDonApi();

            if (dto != null)
            {
                // ✅ Sửa hóa đơn
                Model = dto;

                ChiTietListBox.ItemsSource = Model.ChiTietHoaDons;
                ChiTietListBox.UpdateLayout();
                ChiTietListBox.SelectedIndex = -1;
                ChiTietListBox.SelectedItem = null;
                NoteTuDoTextBox.Text = Model.GhiChu;

                if (Model.KhachHangId != null)
                {
                    var kh = KhachHangSearchBox.KhachHangList
                        .FirstOrDefault(x => x.Id == Model.KhachHangId.Value);

                    if (kh != null)
                    {
                        // ⭐ Hiển thị điểm/thưởng
                        DiemThangNayTextBlock.Text = StarHelper.GetStarText(Model.DiemThangNay);
                        DiemThangTruocTextBlock.Text = StarHelper.GetStarText(Model.DiemThangTruoc);

                        KhachHangSearchBox.SetSelectedKhachHangByIdWithoutPopup(kh.Id);

                        if (Model.KhachHangId == Guid.Parse("D6A1CFA4-E070-4599-92C2-884CD6469BF4"))
                            DiaChiComboBox.Text = Model.DiaChiText;
                        else
                        {  // 🟟 Load địa chỉ & điện thoại y như event
                            var diaChiList = kh.Addresses?.ToList() ?? new();
                            DiaChiComboBox.ItemsSource = diaChiList;
                            DiaChiComboBox.SelectedItem =
                                diaChiList.FirstOrDefault(x => x.DiaChi == Model.DiaChiText)
                                ?? diaChiList.FirstOrDefault(x => x.IsDefault)
                                ?? diaChiList.LastOrDefault();
                        }
                        var sdtList = kh.Phones?.ToList() ?? new();
                        DienThoaiComboBox.ItemsSource = sdtList;
                        DienThoaiComboBox.SelectedItem =
                            sdtList.FirstOrDefault(x => x.SoDienThoai == Model.SoDienThoaiText)
                            ?? sdtList.FirstOrDefault(x => x.IsDefault)
                            ?? sdtList.LastOrDefault();
                    }


                }

                // Voucher
                if (Model.VoucherId != null)
                {
                    VoucherComboBox.SelectedValue = Model.VoucherId;
                    HuyVoucherButton.Visibility = Visibility.Visible;
                }

                // Chọn loại đơn & mở form
                NoiDungForm.IsEnabled = true;
                NoiDungForm.Opacity = 1;

                switch (Model.PhanLoai)
                {
                    case "Tại Chỗ":
                        TaiChoRadio.IsChecked = true;
                        if (!string.IsNullOrWhiteSpace(Model.TenBan))
                        {
                            TenBanComboBox.IsDropDownOpen = false;
                            TenBanComboBox.SelectedItem = Model.TenBan;
                        }
                        break;
                    case "MV":
                        MuaVeRadio.IsChecked = true;
                        break;
                    case "Ship":
                        ShipRadio.IsChecked = true;
                        break;
                    case "App":
                        AppRadio.IsChecked = true;
                        break;
                }


            }
            else
            {
                // ✅ Tạo mới hóa đơn
                Model.MaHoaDon = "HD" + DateTime.Now.Ticks.ToString()[^6..];
            }

            if (Model.IsDeleted)
            {
                SaveButton.Content = "Khôi phục";
                NoiDungForm.IsEnabled = false;
                NoiDungForm.Opacity = 0.5;
            }

            if (Model.ChiTietHoaDons == null)
                Model.ChiTietHoaDons = new List<ChiTietHoaDonDto>();

            if (Model.ChiTietHoaDonToppings == null)
                Model.ChiTietHoaDonToppings = new List<ChiTietHoaDonToppingDto>();

            ChiTietListBox.ItemsSource = Model.ChiTietHoaDons;

            // ✅ Tính lại tổng tiền khi mở form
            CapNhatTongTien();
        }



        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            if (TaiChoRadio.IsChecked == true)
                Model.PhanLoai = "Tại Chỗ";
            else if (MuaVeRadio.IsChecked == true)
                Model.PhanLoai = "MV";
            else if (ShipRadio.IsChecked == true)
                Model.PhanLoai = "Ship";
            else if (AppRadio.IsChecked == true)
                Model.PhanLoai = "App";

            Model.TrangThai = "";
            Model.TenBan = TenBanComboBox.Text;
            Model.KhachHangId = KhachHangSearchBox.SelectedKhachHang?.Id;
            // ✅ Gán số điện thoại và địa chỉ vào Model
            Model.TenKhachHangText = KhachHangSearchBox.SearchTextBox.Text;
            Model.SoDienThoaiText = DienThoaiComboBox.Text?.Trim();
            Model.DiaChiText = DiaChiComboBox.Text?.Trim();
            // ✅ Gửi tên khách hàng mới nếu người dùng nhập tên nhưng chưa chọn khách hàng có sẵn
            if (KhachHangSearchBox.SelectedKhachHang == null
                && !string.IsNullOrWhiteSpace(KhachHangSearchBox.SearchTextBox.Text))
            {
                Model.TenKhachHangText = KhachHangSearchBox.SearchTextBox.Text.Trim();
            }
            Model.VoucherId = (Guid?)VoucherComboBox.SelectedValue;


            Model.ChiTietHoaDonVouchers = new List<ChiTietHoaDonVoucherDto>();

            if (VoucherComboBox.SelectedItem is VoucherDto voucher && voucher.Id != Guid.Empty)
            {
                Model.ChiTietHoaDonVouchers.Add(new ChiTietHoaDonVoucherDto
                {
                    VoucherId = voucher.Id,
                    GiaTriApDung = voucher.GiaTri
                });
            }


            //if (string.IsNullOrWhiteSpace(Model.MaHoaDon))
            //{
            //    ErrorTextBlock.Text = $"Mã hóa đơn không được để trống.";
            //    return;
            //}

            if (TaiChoRadio.IsChecked == true && string.IsNullOrWhiteSpace(Model.TenBan))
            {
                ErrorTextBlock.Text = "Tên bàn không được để trống.";
                TenBanComboBox.IsDropDownOpen = true;
                return;
            }

            if (Model.ChiTietHoaDons.Count == 0)
            {
                ErrorTextBlock.Text = "Chưa có sản phẩm nào trong hóa đơn.";
                return;
            }

            // Xử lý thêm địa chỉ và SĐT mới (UI -> DTO)
            if (KhachHangSearchBox.SelectedKhachHang is KhachHangDto kh)
            {
                string diaChiText = DiaChiComboBox.Text?.Trim() ?? string.Empty;
                var diaChiSelected = DiaChiComboBox.SelectedItem as KhachHangAddressDto;
                Model.DiaChiText = diaChiText;

                if (!string.IsNullOrWhiteSpace(diaChiText))
                {
                    bool diaChiTrung = kh.Addresses.Any(a => a.DiaChi.Equals(diaChiText, StringComparison.OrdinalIgnoreCase));
                    if (!diaChiTrung)
                    {
                        var diaChiMoi = new KhachHangAddressDto
                        {
                            Id = Guid.NewGuid(),
                            DiaChi = diaChiText,
                            IsDefault = false
                        };
                        kh.Addresses.Add(diaChiMoi);
                        DiaChiComboBox.ItemsSource = null;
                        DiaChiComboBox.ItemsSource = kh.Addresses;
                        DiaChiComboBox.SelectedItem = diaChiMoi;
                    }
                }

                string sdtText = DienThoaiComboBox.Text?.Trim() ?? string.Empty;
                var sdtSelected = DienThoaiComboBox.SelectedItem as KhachHangPhoneDto;
                Model.SoDienThoaiText = sdtText;
                if (!string.IsNullOrWhiteSpace(sdtText))
                {
                    bool sdtTrung = kh.Phones.Any(p => p.SoDienThoai.Equals(sdtText, StringComparison.OrdinalIgnoreCase));
                    if (!sdtTrung)
                    {
                        var sdtMoi = new KhachHangPhoneDto
                        {
                            Id = Guid.NewGuid(),
                            SoDienThoai = sdtText,
                            IsDefault = false
                        };
                        kh.Phones.Add(sdtMoi);
                        DienThoaiComboBox.ItemsSource = null;
                        DienThoaiComboBox.ItemsSource = kh.Phones;
                        DienThoaiComboBox.SelectedItem = sdtMoi;
                    }
                }
            }

            // 🟟 Kiểm tra trước khi xoá
            if (!Model.ChiTietHoaDons.Any(ct => ct.SoLuong > 0))
            {
                ErrorTextBlock.Text = "Chưa có sản phẩm nào trong hóa đơn.";
                return;
            }

            // 🟟 Chỉ giữ lại các dòng > 0 để lưu
            Model.ChiTietHoaDons = Model.ChiTietHoaDons
                .Where(ct => ct.SoLuong > 0)
                .ToList();

            // ✅ Đồng bộ lại topping, STT trước khi kiểm tra rỗng
            DongBoTatCaTopping();



            Result<HoaDonDto> result;
            if (Model.Id == Guid.Empty)
                result = await _api.CreateAsync(Model);
            else if (Model.IsDeleted)
                result = await _api.RestoreAsync(Model.Id);
            else
                result = await _api.UpdateAsync(Model.Id, Model);

            if (!result.IsSuccess)
            {
                ErrorTextBlock.Text = result.Message;
                return;
            }

            DialogResult = true;
            Close();
        }

        private void DongBoTatCaTopping()
        {
            foreach (var chiTiet in Model.ChiTietHoaDons)
            {
                // Xóa topping cũ trong Model.ChiTietHoaDonToppings
                if (Model.ChiTietHoaDonToppings is List<ChiTietHoaDonToppingDto> list)
                {
                    list.RemoveAll(tp => tp.ChiTietHoaDonId == chiTiet.Id);
                }

                foreach (var t in chiTiet.ToppingDtos)
                {
                    Model.ChiTietHoaDonToppings.Add(new ChiTietHoaDonToppingDto
                    {
                        ChiTietHoaDonId = chiTiet.Id,
                        ToppingId = t.Id,
                        SoLuong = t.SoLuong,
                        Gia = t.Gia,
                        Ten = t.Ten
                    });
                }
            }
        }

        private void ResetSanPhamInputs()
        {
            // Reset combobox biến thể
            BienTheComboBox.ItemsSource = null;
            BienTheComboBox.SelectedIndex = -1;

            // Reset số lượng
            SoLuongTextBox.Text = "0";

            // Bỏ chọn tất cả radio trong Tab Note
            foreach (var radio in this.FindVisualChildren<RadioButton>().Where(r => r.GroupName != "LoaiDon"))
            {
                radio.IsChecked = false;
            }

            if (ToppingListBox.ItemsSource is List<ToppingDto> ds)
            {
                foreach (var t in ds) t.SoLuong = 0;
                ToppingListBox.Items.Refresh();
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseButton_Click(null!, null!);
                return;
            }

            if (e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement is Button) return;

                var request = new TraversalRequest(FocusNavigationDirection.Next);
                if (Keyboard.FocusedElement is UIElement element)
                {
                    element.MoveFocus(request);
                    e.Handled = true;
                }
            }
        }


        private bool _isAddingFromSearch = false;
        private void BienTheComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isAddingFromSearch) return; // ✅ Không tự thêm lại

            if (SanPhamSearchBox.SelectedSanPham is not SanPhamDto sanPham ||
                BienTheComboBox.SelectedItem is not SanPhamBienTheDto bienThe)
                return;

            //// Nếu chưa có số lượng, mặc định = 1 và thêm sản phẩm
            //if (!int.TryParse(SoLuongTextBox.Text, out int sl) || sl <= 0)
            //{
            //    sl = 1;
            //    SoLuongTextBox.Text = "1";
            //    CapNhatChiTietSanPham(sl);
            //}

            // Lấy dòng hiện tại trong list
            var existing = (!ChiTietListBox.SelectedItems.Contains(null))
           ? ChiTietListBox.SelectedItem as ChiTietHoaDonDto
           : null;

            if (existing == null)
            {
                existing = Model.ChiTietHoaDons.FirstOrDefault(x => x.SanPhamIdBienThe == bienThe.Id);
            }

            if (existing != null)
            {
                // ✅ Cập nhật lại thông tin biến thể và giá
                existing.SanPhamIdBienThe = bienThe.Id;
                existing.TenBienThe = bienThe.TenBienThe;
                existing.DonGia = bienThe.GiaBan;
            }

            // Refresh UI
            ChiTietListBox.Items.Refresh();
            CapNhatTongTien();

            SanPhamSearchBox.SearchTextBox.Focus();
            SanPhamSearchBox.SearchTextBox.SelectAll();

        }

        private void ToppingMinus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ToppingDto topping)
            {
                if (topping.SoLuong > 0) topping.SoLuong--;
                ToppingListBox.Items.Refresh();
                CapNhatToppingChoSanPham();
            }
        }

        private void ToppingPlus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ToppingDto topping)
            {
                topping.SoLuong++;
                ToppingListBox.Items.Refresh();
                CapNhatToppingChoSanPham();
            }
        }
        private void LoadToppingPanel(Guid? nhomSanPhamId)
        {
            var dsTopping = _toppingList
                .Where(t => t.NhomSanPhams.Contains(nhomSanPhamId ?? Guid.Empty))
                .OrderBy(x => x.Ten)
                .Select(t => new ToppingDto
                {
                    Id = t.Id,
                    Ten = t.Ten,
                    Gia = t.Gia,
                    SoLuong = 0 // mặc định 0
                })
                .ToList();

            ToppingListBox.ItemsSource = dsTopping;
        }

        private void CapNhatTongTien()
        {
            decimal tongTien = 0;

            // Tính tổng tiền
            foreach (var ct in Model.ChiTietHoaDons)
            {
                decimal tienTopping = ct.ToppingDtos?.Sum(t => t.Gia * t.SoLuong) ?? 0;
                tongTien += (ct.DonGia * ct.SoLuong) + tienTopping;
            }

            decimal giamGia = 0;

            // ✅ Lấy voucher hiện đang chọn (nếu có)
            if (VoucherComboBox.SelectedItem is VoucherDto voucher)
            {
                giamGia = DiscountHelper.TinhGiamGia(tongTien, voucher.KieuGiam, voucher.GiaTri, lamTron: true);
            }
            else if (Model.GiamGia > 0)
            {
                giamGia = DiscountHelper.TinhGiamGia(tongTien, "fix", Model.GiamGia, lamTron: true);
            }

            if (giamGia > 0)
            {
                var remainder = giamGia % 1000;
                if (remainder < 500)
                    giamGia -= remainder; // làm tròn xuống
                else
                    giamGia += (1000 - remainder); // làm tròn lên
            }

            // ✅ Không vượt quá tổng tiền
            if (giamGia > tongTien)
                giamGia = tongTien;

            // ✅ Cập nhật model trước khi lưu
            Model.TongTien = tongTien;
            Model.GiamGia = giamGia;
            Model.ThanhTien = tongTien - giamGia;

            TongTienTextBlock.Text = Model.TongTien.ToString("N0") + " đ";
            GiamGiaTextBlock.Text = Model.GiamGia.ToString("N0") + " đ";
            ThanhTienTextBlock.Text = Model.ThanhTien.ToString("N0") + " đ";

            TongSoSanPhamTextBlock.Text = Model.ChiTietHoaDons.Sum(x => x.SoLuong).ToString("N0");

        }

        private void CapNhatToppingChoSanPham()
        {
            if (SanPhamSearchBox.SelectedSanPham is not SanPhamDto sanPham ||
                BienTheComboBox.SelectedItem is not SanPhamBienTheDto bienThe)
                return;

            var existing = ChiTietListBox.SelectedItem as ChiTietHoaDonDto
                           ?? Model.ChiTietHoaDons.LastOrDefault(x => x.SanPhamIdBienThe == bienThe.Id);

            if (existing == null) return;

            existing.ToppingDtos.Clear();

            // Xóa topping cũ trong Model.ChiTietHoaDonToppings
            foreach (var item in Model.ChiTietHoaDonToppings
                     .Where(tp => tp.ChiTietHoaDonId == existing.Id).ToList())
            {
                Model.ChiTietHoaDonToppings.Remove(item);
            }

            if (ToppingListBox.ItemsSource is List<ToppingDto> ds)
            {
                foreach (var t in ds.Where(x => x.SoLuong > 0))
                {
                    existing.ToppingDtos.Add(new ToppingDto
                    {
                        Id = t.Id,
                        Ten = t.Ten,
                        Gia = t.Gia,
                        SoLuong = t.SoLuong
                    });

                    Model.ChiTietHoaDonToppings.Add(new ChiTietHoaDonToppingDto
                    {
                        ChiTietHoaDonId = existing.Id,
                        ToppingId = t.Id,
                        SoLuong = t.SoLuong,
                        Gia = t.Gia,
                        Ten = t.Ten
                    });
                }
            }

            existing.ToppingText = existing.ToppingDtos.Any()
                ? string.Join(", ", existing.ToppingDtos.Select(tp => $"{tp.Ten} x{tp.SoLuong}"))
                : "";

            ChiTietListBox.Items.Refresh();
            CapNhatTongTien();

            ChiTietListBox.SelectedItem = existing;
            ChiTietListBox.ScrollIntoView(existing);
            SanPhamSearchBox.SearchTextBox.Focus();
            SanPhamSearchBox.SearchTextBox.SelectAll();
        }
        private void XoaChiTietButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ChiTietHoaDonDto ct) return;

            if (MessageBox.Show($"Xoá {ct.TenSanPham}?", "Xác nhận", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            Model.ChiTietHoaDons.Remove(ct);
            ChiTietListBox.Items.Refresh();
            CapNhatTongTien();
        }



        private void LoaiDonRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                NoiDungForm.IsEnabled = true;
                NoiDungForm.Opacity = 1;

                // Xử lý bàn
                if (rb == TaiChoRadio)
                {
                    TenBanComboBox.Visibility = Visibility.Visible;
                    TenBanComboBox.IsDropDownOpen = true;
                }
                else
                {
                    TenBanComboBox.Visibility = Visibility.Collapsed;
                    TenBanComboBox.SelectedItem = null;
                }

                // Xử lý focus cho Ship / mặc định
                if (rb == ShipRadio)
                {
                    KhachHangSearchBox.SearchTextBox.Focus();
                }
                else
                {
                    SanPhamSearchBox.SearchTextBox.Focus();
                }

                // Xử lý voucher cho "App"
                if (rb == AppRadio && VoucherComboBox.Items.Count > 0)
                {
                    VoucherComboBox.SelectedIndex = VoucherComboBox.Items.Count - 1;
                }
                else
                {
                    VoucherComboBox.SelectedIndex = -1;
                }

            }

        }

        private void HuyVoucher_Click(object sender, RoutedEventArgs e)
        {
            VoucherComboBox.SelectedIndex = -1;
            Model.VoucherId = null;
            HuyVoucherButton.Visibility = Visibility.Collapsed;
            CapNhatTongTien();
        }

        private void VoucherComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Model.ChiTietHoaDonVouchers == null)
                Model.ChiTietHoaDonVouchers = new List<ChiTietHoaDonVoucherDto>();

            // Khi có voucher được chọn
            if (VoucherComboBox.SelectedItem is VoucherDto selectedVoucher)
            {
                Model.VoucherId = selectedVoucher.Id;

                // Xóa các voucher cũ để chỉ giữ 1 voucher hiện tại
                Model.ChiTietHoaDonVouchers.Clear();

                // Chỉ gửi dữ liệu cần thiết (VoucherId và GiaTriApDung)
                Model.ChiTietHoaDonVouchers.Add(new ChiTietHoaDonVoucherDto
                {
                    VoucherId = selectedVoucher.Id,
                    GiaTriApDung = selectedVoucher.GiaTri // nếu có, có thể tùy chỉnh
                });

                HuyVoucherButton.Visibility = Visibility.Visible;
            }
            else
            {
                // Khi bỏ chọn voucher
                Model.VoucherId = null;

                if (Model.ChiTietHoaDonVouchers != null)
                    Model.ChiTietHoaDonVouchers.Clear();

                HuyVoucherButton.Visibility = Visibility.Collapsed;
            }

            CapNhatTongTien();
        }

        private void TangButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(SoLuongTextBox.Text, out int sl)) sl = 0;
            sl++;
            SoLuongTextBox.Text = sl.ToString();
            CapNhatChiTietSanPham(sl);
        }

        private void GiamButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(SoLuongTextBox.Text, out int sl)) sl = 0;
            sl = Math.Max(0, sl - 1);
            SoLuongTextBox.Text = sl.ToString();
            CapNhatChiTietSanPham(sl);
        }
        private void CapNhatChiTietSanPham(int soLuong, bool forceNewLine = false)
        {
            if (SanPhamSearchBox.SelectedSanPham is not SanPhamDto sanPham ||
                BienTheComboBox.SelectedItem is not SanPhamBienTheDto bienThe)
                return;

            string currentNote = string.Join(", ", this.FindVisualChildren<RadioButton>()
                .Where(r => r.IsChecked == true && r.GroupName != "LoaiDon")
                .Select(r => r.Content.ToString()));

            ChiTietHoaDonDto? existing = null;

            // 🟟 Chỉ dùng dòng đang chọn nếu KHÔNG ép thêm mới
            if (!forceNewLine)
            {
                existing = ChiTietListBox.SelectedItem as ChiTietHoaDonDto
                           ?? Model.ChiTietHoaDons
                                .FirstOrDefault(x => x.SanPhamIdBienThe == bienThe.Id);
            }



            if (soLuong == 0)
            {
                if (existing != null)
                {
                    Model.ChiTietHoaDons.Remove(existing);
                    ChiTietListBox.Items.Refresh();
                    CapNhatTongTien();
                }
            }
            else
            {
                if (existing == null)
                {
                    // ✅ Luôn tạo dòng mới nếu forceNewLine = true hoặc không tìm thấy dòng phù hợp
                    existing = new ChiTietHoaDonDto
                    {
                        SanPhamIdBienThe = bienThe.Id,
                        TenSanPham = sanPham.Ten,
                        TenBienThe = bienThe.TenBienThe,
                        DonGia = bienThe.GiaBan,
                        SoLuong = soLuong,
                        BienTheList = _bienTheList.Where(bt => bt.SanPhamId == sanPham.Id).ToList(),
                        ToppingDtos = new List<ToppingDto>(),
                        //NoteText = $"{currentNote}"
                    };
                    Model.ChiTietHoaDons.Add(existing);
                    CapNhatToppingChoSanPham(); // ✅ đồng bộ topping ngay khi thêm sản phẩm mới
                }
                else
                {
                    // ✅ Cập nhật số lượng dòng đang chọn
                    existing.SoLuong = soLuong;
                }

                // Đánh lại STT
                int stt = 1;
                foreach (var ct in Model.ChiTietHoaDons)
                {
                    ct.Stt = stt++;
                }

                ChiTietListBox.Items.Refresh();
                CapNhatTongTien();

                ChiTietListBox.SelectedItem = existing;
                ChiTietListBox.ScrollIntoView(existing);
                SanPhamSearchBox.SearchTextBox.Focus();
                SanPhamSearchBox.SearchTextBox.SelectAll();
            }

        }

        private void RadioButton_PreviewMouseLeftButtonDown_Common(object sender, MouseButtonEventArgs e)
        {
            if (sender is RadioButton rb && rb.IsChecked == true)
            {
                rb.IsChecked = false;
                e.Handled = true;
            }
        }
        private void NoteRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_isLoadingNote) return;
            CapNhatNoteChoSanPham();
        }
        private void TenBanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SanPhamSearchBox.SearchTextBox.Focus();

        }
        private bool _isLoadingNote = false;
        private void ChiTietListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChiTietListBox.SelectedItem is not ChiTietHoaDonDto selected)
            {
                ToppingGroupBox.Visibility = Visibility.Collapsed;
                return;
            }
            ToppingGroupBox.Visibility = Visibility.Visible;


            _isLoadingNote = true;

            // Bỏ chọn tất cả radio trước
            foreach (var radio in this.FindVisualChildren<RadioButton>().Where(r => r.GroupName != "LoaiDon"))
                radio.IsChecked = false;

            _isLoadingNote = false;

            // ✅ Chọn sản phẩm tương ứng
            var sanPham = _sanPhamList.FirstOrDefault(sp => sp.BienThe.Any(bt => bt.Id == selected.SanPhamIdBienThe));
            if (sanPham != null)
            {
                SanPhamSearchBox.SuppressPopup = true;
                SanPhamSearchBox.SetSelectedSanPham(sanPham);
                SanPhamSearchBox.SuppressPopup = false;

                // ✅ Luôn nạp lại danh sách biến thể từ _bienTheList
                var bienThes = _bienTheList.Where(x => x.SanPhamId == sanPham.Id).ToList();
                BienTheComboBox.ItemsSource = bienThes;
                BienTheComboBox.SelectedValue = selected.SanPhamIdBienThe;

                // ✅ Cập nhật số lượng topping
                LoadToppingPanel(sanPham.NhomSanPhamId);
            }

            if (ToppingListBox.ItemsSource is List<ToppingDto> ds)
            {
                foreach (var t in ds)
                {
                    var match = selected.ToppingDtos.FirstOrDefault(x => x.Id == t.Id);
                    t.SoLuong = match?.SoLuong ?? 0;
                }
                ToppingListBox.Items.Refresh();
            }

            // ✅ Cập nhật note
            var allNotes = selected.NoteText?.Split('#')
                .Select(x => x.Trim())
                .ToList() ?? new List<string>();

            // Tick các radio tương ứng
            foreach (var radio in this.FindVisualChildren<RadioButton>().Where(r => r.GroupName != "LoaiDon"))
            {
                radio.IsChecked = allNotes?.Contains(radio.Content?.ToString() ?? "") ?? false;
            }

            // Hiển thị note tự do (không nằm trong radio)
            var predefinedNotes = this.FindVisualChildren<RadioButton>()
                .Select(r => r.Content.ToString())
                .ToHashSet();

            var freeNotes = allNotes?
      .Where(n => !predefinedNotes.Contains(n))
      ?? Enumerable.Empty<string>();
            NoteTuDoTextBox.Text = string.Join(" # ", freeNotes);

            // ✅ Cập nhật số lượng
            SoLuongTextBox.Text = selected.SoLuong.ToString();

        }
        private void ThemDongButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietListBox.SelectedItem is not ChiTietHoaDonDto selected)
            {
                MessageBox.Show("Vui lòng chọn món để thêm dòng mới.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ✅ Ép buộc tạo dòng mới
            CapNhatChiTietSanPham(1, true);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
        private void NoteTuDoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingNote) return;
            CapNhatNoteChoSanPham();
        }
        private void CapNhatNoteChoSanPham()
        {
            if (ChiTietListBox.SelectedItem is ChiTietHoaDonDto selected)
            {
                // Lấy note từ radio
                var selectedNotes = this.FindVisualChildren<RadioButton>()
                    .Where(r => r.IsChecked == true && r.GroupName != "LoaiDon")
                    .Select(r => r.Content.ToString())
                    .ToList();

                // Lấy note tự do
                var noteTuDo = NoteTuDoTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(noteTuDo))
                    selectedNotes.Add(noteTuDo);

                // Ghép bằng dấu #
                selected.NoteText = selectedNotes.Any()
                    ? string.Join(" # ", selectedNotes)
                    : "";

                ChiTietListBox.Items.Refresh();
            }
        }
        private void DiemThangTruocTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!Model.DuocNhanVoucher)
            {
                MessageBox.Show("Khách hàng này không thuộc diện được nhận voucher.", "Thông báo");
                return;
            }

            if (Model.DaNhanVoucher)
            {
                MessageBox.Show("Khách hàng đã nhận voucher trong tháng này rồi.", "Thông báo");
                return;
            }

            // 🟟 Tính số sao đầy và giá trị voucher dựa trên LoyaltyHelper
            int saoDay = LoyaltyHelper.TinhSoSaoDay(Model.DiemThangTruoc);
            int giaTriVoucher = LoyaltyHelper.TinhGiaTriVoucher(Model.DiemThangTruoc);

            if (saoDay > 0 && giaTriVoucher > 0)
            {
                if (VoucherComboBox.ItemsSource is IEnumerable<VoucherDto> vouchers)
                {
                    var voucher = vouchers.FirstOrDefault(v => v.GiaTri == giaTriVoucher);
                    if (voucher != null)
                    {
                        VoucherComboBox.SelectedItem = voucher;
                        HuyVoucherButton.Visibility = Visibility.Visible;
                        MessageBox.Show(
                            $"Khách đủ {saoDay} sao → được nhận voucher {giaTriVoucher:N0} đ.",
                            "Thông báo",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                        return;
                    }
                }
                MessageBox.Show(
                    $"Khách đủ {saoDay} sao nhưng không tìm thấy voucher {giaTriVoucher:N0} đ trong danh sách.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
            else
            {
                MessageBox.Show("Khách chưa đủ điểm để nhận voucher.", "Thông báo");
            }
        }

    }
}

