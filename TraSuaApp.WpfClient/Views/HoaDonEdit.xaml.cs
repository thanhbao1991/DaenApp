using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using TraSuaApp.Shared.Config;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.AiOrdering;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Controls;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.HoaDonViews
{
    public partial class HoaDonEdit : Window
    {
        public HoaDonDto Model { get; set; } = new();
        private readonly HoaDonApi _api;
        string _friendlyName = TuDien._tableFriendlyNames["HoaDon"];

        public Guid? SavedHoaDonId { get; internal set; }
        private readonly QuickOrderService _quick = new(Config.apiChatGptKey);
        private bool _openedFromMessenger;
        private bool _messengerInputIsImage = false; // chỉ true khi mở từ Messenger và input là ảnh
        private List<SanPhamDto> _sanPhamList = new();
        private List<SanPhamBienTheDto> _bienTheList = new();
        private List<ToppingDto> _toppingList = new();
        private List<VoucherDto> _voucherList = new();
        private ObservableCollection<KhachHangDto> _khachHangsList = new();

        private readonly string[] _banList = new[]
        {
            "2", "3", "4", "5", "6",
            "7", "8", "9", "10", "13",
            "Sân 1", "Sân 2"
        };


        private bool _isLoadingNote = false;

        private static class Pricing
        {
            public static decimal CalcVoucherDiscount(decimal tongTien, VoucherDto? voucher, decimal giamGiaFix)
            {
                decimal giamGia = 0;
                if (voucher != null)
                    giamGia = DiscountHelper.TinhGiamGia(tongTien, voucher.KieuGiam, voucher.GiaTri, lamTron: true);
                else if (giamGiaFix > 0)
                    giamGia = DiscountHelper.TinhGiamGia(tongTien, "fix", giamGiaFix, lamTron: true);

                if (giamGia > 0)
                {
                    var remainder = giamGia % 1000;
                    giamGia = remainder < 500 ? giamGia - remainder : giamGia + (1000 - remainder);
                }
                return Math.Min(giamGia, tongTien);
            }

            public static void ApplyCustomerPricingForAllLines(
                ObservableCollection<ChiTietHoaDonDto> lines,
                Guid khId,
                IEnumerable<KhachHangGiaBanDto> khGiaBans,
                Action<string>? showMessage // truyền null để không popup
            )
            {

                var dsMonCapNhat = new List<string>();
                foreach (var ct in lines)
                {
                    var customGia = khGiaBans.FirstOrDefault(x => x.KhachHangId == khId
                                                               && x.SanPhamBienTheId == ct.SanPhamIdBienThe
                                                               && !x.IsDeleted);
                    if (customGia != null && ct.DonGia != customGia.GiaBan)
                    {
                        ct.DonGia = customGia.GiaBan;
                        dsMonCapNhat.Add($"{ct.TenSanPham} ({ct.DonGia:N0})");
                    }
                }
                if (showMessage != null && dsMonCapNhat.Any())
                {
                    showMessage("Đã cập nhật giá riêng cho các món:\n- " + string.Join("\n- ", dsMonCapNhat));
                }
            }
        }

        private static class ToppingSync
        {
            public static void SyncAll(ObservableCollection<ChiTietHoaDonDto> chiTiet, ICollection<ChiTietHoaDonToppingDto> target)
            {
                if (target == null) return;

                // Xóa các record orphan (không thuộc dòng nào trong bill hiện tại)
                var toRemoveOrphans = target.Where(tp => chiTiet.All(ct => ct.Id != tp.ChiTietHoaDonId)).ToList();
                foreach (var r in toRemoveOrphans) target.Remove(r);

                foreach (var ct in chiTiet)
                {
                    // Xóa topping cũ của dòng này
                    foreach (var old in target.Where(tp => tp.ChiTietHoaDonId == ct.Id).ToList())
                        target.Remove(old);

                    // Thêm topping hiện tại
                    foreach (var t in ct.ToppingDtos)
                    {
                        target.Add(new ChiTietHoaDonToppingDto
                        {
                            ChiTietHoaDonId = ct.Id,
                            ToppingId = t.Id,
                            SoLuong = t.SoLuong,
                            Gia = t.Gia,
                            Ten = t.Ten
                        });
                    }
                }
            }
        }
        private ChiTietHoaDonDto? GetSelectedOrLastLine()
            => (ChiTietListBox.SelectedItem as ChiTietHoaDonDto) ?? Model.ChiTietHoaDons.LastOrDefault();

        private void Resequence()
        {
            int stt = 1;
            foreach (var item in Model.ChiTietHoaDons)
                item.Stt = stt++;
        }

        private void TryAutoPickCustomerFromMessenger(string? rawName)
        {
            try
            {
                if (!_openedFromMessenger) return;
                if (_messengerInputIsImage) return;
                if (string.IsNullOrWhiteSpace(rawName)) return;

                var name = rawName.Trim();

                // 1) Exact match
                var exact = _khachHangsList.FirstOrDefault(x =>
                    string.Equals((x.Ten ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase));

                if (exact != null)
                {
                    KhachHangSearchBox.SuppressPopup = true;
                    KhachHangSearchBox.SetSelectedKhachHang(exact);
                    KhachHangSearchBox.SuppressPopup = false;
                    KhachHangSearchBox.IsPopupOpen = false;           // 🟟 không mở popup
                    KhachHangSearchBox.TriggerSelectedEvent(exact);   // chạy pipeline
                    return;
                }

                // 2) Fallback: 1 kết quả duy nhất theo TimKiem
                var key = StringHelper.MyNormalizeText(name);
                var matches = _khachHangsList
                    .Where(x => x.TimKiem.Contains(key))
                    .OrderByDescending(x => x.ThuTu)
                    .Take(15)
                    .ToList();

                if (matches.Count == 1)
                {
                    var kh = matches[0];
                    KhachHangSearchBox.SuppressPopup = true;
                    KhachHangSearchBox.SetSelectedKhachHang(kh);
                    KhachHangSearchBox.SuppressPopup = false;
                    KhachHangSearchBox.IsPopupOpen = false;           // 🟟 không mở popup
                    KhachHangSearchBox.TriggerSelectedEvent(kh);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TryAutoPickCustomerFromMessenger error: " + ex.Message);
            }
        }

        public HoaDonEdit(HoaDonDto? dto = null)
        {
            InitializeComponent();
            AnimationHelper.FadeInWindow(this); // 🟟 mở mượt

            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _sanPhamList = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();
            _bienTheList = _sanPhamList.SelectMany(x => x.BienThe).ToList();
            _toppingList = AppProviders.Toppings.Items.ToList();
            _voucherList = AppProviders.Vouchers.Items.ToList();
            _khachHangsList = AppProviders.KhachHangs.Items;

            // === Wire SanPhamSearchBox ===
            SanPhamSearchBox.SanPhamList = _sanPhamList;
            SanPhamSearchBox.SanPhamCleared += () => ResetSanPhamInputs();
            SanPhamSearchBox.SanPhamBienTheSelected += (sanPham, bienThe) =>
            {
                if (sanPham == null || bienThe == null) return;

                ResetSanPhamInputs();

                var ct = new ChiTietHoaDonDto
                {
                    Id = Guid.NewGuid(),

                    LastModified = DateTime.Now,
                    SanPhamIdBienThe = bienThe.Id,
                    TenSanPham = sanPham.Ten,
                    TenBienThe = bienThe.TenBienThe,
                    SoLuong = 1,
                    Stt = 0,
                    BienTheList = _bienTheList.Where(x => x.SanPhamId == sanPham.Id).ToList(),
                    ToppingDtos = new List<ToppingDto>()
                };

                decimal donGia = bienThe.GiaBan;
                if (Model.KhachHangId != null && !string.Equals(Model.PhanLoai, "App", StringComparison.OrdinalIgnoreCase))

                {
                    var customGia = AppProviders.KhachHangGiaBans.Items
                    .FirstOrDefault(x => x.KhachHangId == Model.KhachHangId.Value
                                      && x.SanPhamBienTheId == bienThe.Id
                                      && !x.IsDeleted);
                    if (customGia != null)
                    {
                        donGia = customGia.GiaBan;
                        MessageBox.Show(
                            $"Đã áp dụng giá riêng cho món: {sanPham.Ten}",
                            "Thông báo",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                }
                ct.DonGia = donGia;

                AttachLineWatcher(ct);

                Model.ChiTietHoaDons.Add(ct);
                LoadToppingPanel(sanPham.NhomSanPhamId);

                FocusLine(ct);

                SanPhamSearchBox.SearchTextBox.Focus();
                SanPhamSearchBox.SearchTextBox.SelectAll();
            };

            // === Wire KhachHangSearchBox ===
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

                VoucherComboBox.IsEnabled = true;

                try
                {
                    var result = await _api.GetKhachHangInfoAsync(kh.Id);
                    if (result.IsSuccess == true && result.Data != null)
                    {
                        var info = result.Data;

                        Model.DiemThangNay = info.DiemThangNay;
                        Model.DiemThangTruoc = info.DiemThangTruoc;
                        Model.TongNoKhachHang = info.TongNo;
                        Model.DaNhanVoucher = info.DaNhanVoucher;

                        CongNoTextBlock.Text = info.TongNo.ToString("N0");
                        DonKhacTextBlock.Text = info.DonKhac.ToString("N0");

                        if (info.TongNo > 0)
                            MessageBox.Show($"Công nợ: {CongNoTextBlock.Text}");

                        if (info.DonKhac > 0)
                            MessageBox.Show($"Đơn khác: {DonKhacTextBlock.Text}");

                        DiemThangNayTextBlock.Text = StarHelper.GetStarText(info.DiemThangNay);
                        DiemThangTruocTextBlock.Text = StarHelper.GetStarText(info.DiemThangTruoc);

                        if (info.DuocNhanVoucher && !info.DaNhanVoucher)
                        {
                            int saoDayTruoc = LoyaltyHelper.TinhSoSaoDay(info.DiemThangTruoc);
                            int giaTriVoucher = LoyaltyHelper.TinhGiaTriVoucher(info.DiemThangTruoc);

                            if (saoDayTruoc > 0 && giaTriVoucher > 0)
                            {
                                var blink = (Storyboard)FindResource("BlinkAnimation");
                                Storyboard.SetTarget(blink, DiemGroupBox);
                                blink.Begin();

                                MessageBox.Show($"Voucher {giaTriVoucher:N0}");
                            }
                        }

                        // Gợi ý món yêu thích
                        RenderFavoriteChipsFromText(info.MonYeuThich);

                        UpdateTotals();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi tải thông tin khách hàng: " + ex.Message);
                }

                // Cập nhật giá riêng cho các dòng đã có
                ApplyCustomerPricingForAllLines(kh.Id, showMessage: true);

                SanPhamSearchBox.SearchTextBox.Focus();
            };
            KhachHangSearchBox.KhachHangCleared += () =>
            {
                Model.KhachHang = null;
                Model.KhachHangId = null;
                DiaChiComboBox.ItemsSource = null;
                DienThoaiComboBox.ItemsSource = null;
                CongNoTextBlock.Text = null;
                DiemThangNayTextBlock.Text = null;
                DiemThangTruocTextBlock.Text = null;
                GoiYWrap.Children.Clear();
            };

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

                if (Model.KhachHangId != null)
                {
                    var kh = KhachHangSearchBox.KhachHangList
                        .FirstOrDefault(x => x.Id == Model.KhachHangId.Value);

                    if (kh != null)
                    {
                        DiemThangNayTextBlock.Text = StarHelper.GetStarText(Model.DiemThangNay);
                        DiemThangTruocTextBlock.Text = StarHelper.GetStarText(Model.DiemThangTruoc);

                        KhachHangSearchBox.SetSelectedKhachHangByIdWithoutPopup(kh.Id);

                        if (Model.KhachHangId == Guid.Parse("D6A1CFA4-E070-4599-92C2-884CD6469BF4"))
                            DiaChiComboBox.Text = Model.DiaChiText;
                        else
                        {
                            var diaChiList = kh.Addresses?.ToList() ?? new();
                            DiaChiComboBox.ItemsSource = diaChiList;
                            DiaChiComboBox.SelectedItem =
                                diaChiList.FirstOrDefault(x => x.DiaChi.EqualsNormalized(Model.DiaChiText))
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
                else
                {
                    VoucherComboBox.SelectedIndex = -1;
                    HuyVoucherButton.Visibility = Visibility.Collapsed;
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
                    case "Mv":
                        MuaVeRadio.IsChecked = true;
                        break;
                    case "Ship":
                        ShipRadio.IsChecked = true;
                        break;
                    case "App":
                        AppRadio.IsChecked = true;
                        break;
                }

                NoteTuDoTextBox.Text = dto.GhiChu;
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
                Model.ChiTietHoaDons = new ObservableCollection<ChiTietHoaDonDto>();

            if (Model.ChiTietHoaDonToppings == null)
                Model.ChiTietHoaDonToppings = new List<ChiTietHoaDonToppingDto>();

            ChiTietListBox.ItemsSource = Model.ChiTietHoaDons;

            // ✅ Tính lại tổng tiền khi mở form
            UpdateTotals();





            foreach (var ct in Model.ChiTietHoaDons) AttachLineWatcher(ct);
            Model.ChiTietHoaDons.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (ChiTietHoaDonDto added in e.NewItems)
                        AttachLineWatcher(added);

                Resequence();
                UpdateTotals();
            };
        }

        private void AttachLineWatcher(ChiTietHoaDonDto ct)
        {
            ct.PropertyChanged -= Line_PropertyChanged;
            ct.PropertyChanged += Line_PropertyChanged;
        }

        private void Line_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChiTietHoaDonDto.SoLuong) ||
                e.PropertyName == nameof(ChiTietHoaDonDto.DonGia) ||
                e.PropertyName == nameof(ChiTietHoaDonDto.ThanhTien) ||
                e.PropertyName == nameof(ChiTietHoaDonDto.ToppingDtos))
            {
                UpdateTotals();
            }
        }

        public HoaDonEdit(HoaDonDto? dto, string? gptInput, string? latestCustomerName, bool openedFromMessenger)
            : this(dto) // gọi lại constructor gốc để khởi tạo UI/bindings
        {
            _openedFromMessenger = openedFromMessenger;

            // 🟟 Xác định input từ Messenger là ảnh hay text (để chỉ auto-pick cho TEXT)
            _messengerInputIsImage = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(gptInput))
                {
                    if (File.Exists(gptInput))
                    {
                        var ext = Path.GetExtension(gptInput).ToLowerInvariant();
                        _messengerInputIsImage = ext == ".png" || ext == ".jpg" || ext == ".jpeg";
                    }
                }
            }
            catch { /* ignore */ }

            // mới:
            if (!string.IsNullOrWhiteSpace(latestCustomerName))
                KhachHangSearchBox.SetTextWithoutPopup(latestCustomerName);

            // 🟟 Auto-pick KH theo tên chat (EXACT → 1 kết quả) TRƯỚC khi chạy GPT
            this.ContentRendered += async (_, __) =>
            {
                TryAutoPickCustomerFromMessenger(latestCustomerName);
                await RunGptFromMessengerIfNeededAsync(latestCustomerName, gptInput);
            };



        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseButton_Click(null!, null!);
                return;
            }
            else if (e.Key == Key.Enter)
            {
                if (KhachHangSearchBox.IsPopupOpen || SanPhamSearchBox.IsPopupOpen) return;
                if (_isSaving) return;
                e.Handled = true;
                SaveButton_Click(SaveButton, new RoutedEventArgs());
            }
        }
        private void UpdateTotals()
        {
            try
            {
                decimal tongTien = 0;

                // ── 1. Tính tổng tiền (bao gồm topping)
                foreach (var ct in Model.ChiTietHoaDons)
                {
                    decimal tienTopping = ct.ToppingDtos?.Sum(t => t.Gia * t.SoLuong) ?? 0;
                    tongTien += (ct.DonGia * ct.SoLuong) + tienTopping;
                }

                // ── 2. Tính tổng số sản phẩm (dùng chung logic)
                int tongSoSanPham = HoaDonCalculator.TinhTongSoSanPham(
                    Model.ChiTietHoaDons,
                    _sanPhamList
                );

                // ── 3. Tính giảm giá
                var currVoucher = VoucherComboBox.SelectedItem as VoucherDto;

                decimal giamGia = Pricing.CalcVoucherDiscount(
                    tongTien,
                    currVoucher,
                    Model.GiamGia
                );

                // ── 4. Gán lại model
                Model.TongTien = tongTien;
                Model.GiamGia = giamGia;
                Model.ThanhTien = tongTien - giamGia;

                // ── 5. Update UI
                TongTienTextBlock.Text = $"{Model.TongTien:N0} đ";
                GiamGiaTextBlock.Text = $"{Model.GiamGia:N0} đ";
                ThanhTienTextBlock.Text = $"{Model.ThanhTien:N0} đ";

                TongSoSanPhamTextBlock.Text = tongSoSanPham.ToString("N0");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UpdateTotals error: " + ex.Message);
            }
        }
        //private void UpdateTotals()
        //{
        //    decimal tongTien = 0;
        //    int tongSoSanPham = 0;

        //    foreach (var ct in Model.ChiTietHoaDons)
        //    {
        //        decimal tienTopping = ct.ToppingDtos?.Sum(t => t.Gia * t.SoLuong) ?? 0;
        //        tongTien += (ct.DonGia * ct.SoLuong) + tienTopping;

        //        var bienThe = _bienTheList.FirstOrDefault(bt => bt.Id == ct.SanPhamIdBienThe);
        //        var sp = _sanPhamList.FirstOrDefault(s => s.Id == bienThe?.SanPhamId);

        //        if (sp != null &&
        //            sp.TenNhomSanPham != "Thuốc lá" &&
        //            sp.TenNhomSanPham != "Nước lon" &&
        //            sp.TenNhomSanPham != "Ăn vặt")
        //        {
        //            tongSoSanPham += ct.SoLuong;
        //        }
        //    }

        //    var currVoucher = VoucherComboBox.SelectedItem as VoucherDto;
        //    decimal giamGia = Pricing.CalcVoucherDiscount(tongTien, currVoucher, Model.GiamGia);

        //    Model.TongTien = tongTien;
        //    Model.GiamGia = giamGia;
        //    Model.ThanhTien = tongTien - giamGia;

        //    TongTienTextBlock.Text = $"{Model.TongTien:N0} đ";
        //    GiamGiaTextBlock.Text = $"{Model.GiamGia:N0} đ";
        //    ThanhTienTextBlock.Text = $"{Model.ThanhTien:N0} đ";

        //    TongSoSanPhamTextBlock.Text = tongSoSanPham.ToString("N0");
        //}

        private void CapNhatToppingChoSanPham()
        {
            var existing = GetSelectedOrLastLine();
            if (existing == null) return;

            existing.ToppingDtos.Clear();

            // Xóa topping cũ trong Model.ChiTietHoaDonToppings
            if (Model.ChiTietHoaDonToppings != null)
            {
                foreach (var item in Model.ChiTietHoaDonToppings.Where(tp => tp.ChiTietHoaDonId == existing.Id).ToList())
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
            UpdateTotals();

            FocusLine(existing);
            SanPhamSearchBox.SearchTextBox.Focus();
            SanPhamSearchBox.SearchTextBox.SelectAll();
        }
        private void FocusLine(ChiTietHoaDonDto ct)
        {
            ChiTietListBox.SelectedItem = ct;
            ChiTietListBox.ScrollIntoView(ct);
        }
        private void ResetSanPhamInputs()
        {
            _isLoadingNote = true;

            try
            {
                foreach (var radio in this.FindVisualChildren<RadioButton>().Where(r => r.GroupName != "LoaiDon"))
                    radio.IsChecked = false;

                NoteTuDoTextBox.Text = string.Empty;

                if (ToppingListBox.ItemsSource is List<ToppingDto> ds)
                {
                    foreach (var t in ds) t.SoLuong = 0;
                    ToppingListBox.Items.Refresh();
                }
            }
            finally
            {
                _isLoadingNote = false;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void XoaChiTietButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ChiTietHoaDonDto ct) return;
            Model.ChiTietHoaDons.Remove(ct);
            ChiTietListBox.Items.Refresh();
            UpdateTotals();
        }

        private void LoaiDonRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                NoiDungForm.IsEnabled = true;
                NoiDungForm.Opacity = 1;

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

                if (rb == ShipRadio)
                    KhachHangSearchBox.SearchTextBox.Focus();
                else
                    SanPhamSearchBox.SearchTextBox.Focus();

                if (rb == AppRadio && VoucherComboBox.Items.Count > 0)
                    VoucherComboBox.SelectedIndex = VoucherComboBox.Items.Count - 1;
            }
        }
        private SanPhamDto? FindSanPhamByNameLoose(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            var q = name.Trim();

            // 1) exact (case-insensitive)
            var exact = _sanPhamList.FirstOrDefault(x =>
                string.Equals(x.Ten?.Trim(), q, StringComparison.OrdinalIgnoreCase));
            if (exact != null) return exact;

            // 2) startsWith
            var starts = _sanPhamList.FirstOrDefault(x =>
                (x.Ten ?? "").Trim().StartsWith(q, StringComparison.OrdinalIgnoreCase));
            if (starts != null) return starts;

            // 3) contains
            var contains = _sanPhamList.FirstOrDefault(x =>
                (x.Ten ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            return contains;
        }
        private void HuyVoucher_Click(object sender, RoutedEventArgs e)
        {
            VoucherComboBox.SelectedIndex = -1;
            Model.VoucherId = null;
            HuyVoucherButton.Visibility = Visibility.Collapsed;
            UpdateTotals();
        }
        private void VoucherComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Model.ChiTietHoaDonVouchers == null)
                Model.ChiTietHoaDonVouchers = new List<ChiTietHoaDonVoucherDto>();

            if (VoucherComboBox.SelectedItem is VoucherDto selectedVoucher)
            {
                Model.VoucherId = selectedVoucher.Id;
                Model.ChiTietHoaDonVouchers.Clear();
                Model.ChiTietHoaDonVouchers.Add(new ChiTietHoaDonVoucherDto
                {
                    VoucherId = selectedVoucher.Id,
                    GiaTriApDung = selectedVoucher.GiaTri
                });
                HuyVoucherButton.Visibility = Visibility.Visible;
            }
            else
            {
                Model.VoucherId = null;
                Model.ChiTietHoaDonVouchers?.Clear();
                HuyVoucherButton.Visibility = Visibility.Collapsed;
            }

            UpdateTotals();
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

        private void ChiTietListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChiTietListBox.SelectedItem is not ChiTietHoaDonDto selected)
            {
                ToppingGroupBox.Visibility = Visibility.Collapsed;
                return;
            }
            ToppingGroupBox.Visibility = Visibility.Visible;

            _isLoadingNote = true;
            foreach (var radio in this.FindVisualChildren<RadioButton>().Where(r => r.GroupName != "LoaiDon"))
                radio.IsChecked = false;
            _isLoadingNote = false;

            var sanPham = _sanPhamList.FirstOrDefault(sp => sp.BienThe.Any(bt => bt.Id == selected.SanPhamIdBienThe));
            if (sanPham != null)
            {
                SanPhamSearchBox.SuppressPopup = true;
                SanPhamSearchBox.SetSelectedSanPham(sanPham);
                SanPhamSearchBox.SuppressPopup = false;

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

            var allNotes = selected.NoteText?.Split('#').Select(x => x.Trim()).ToList() ?? new List<string>();

            foreach (var radio in this.FindVisualChildren<RadioButton>().Where(r => r.GroupName != "LoaiDon"))
                radio.IsChecked = allNotes?.Contains(radio.Content?.ToString() ?? "") ?? false;

            var predefinedNotes = this.FindVisualChildren<RadioButton>()
                .Select(r => r.Content.ToString())
                .ToHashSet();

            var freeNotes = allNotes?.Where(n => !predefinedNotes.Contains(n)) ?? Enumerable.Empty<string>();
            NoteTuDoTextBox.Text = string.Join(" # ", freeNotes);
        }

        private void ApplyCustomerPricingForAllLines(Guid khId, bool showMessage = true)
        {
            // Nếu đơn là App => bỏ qua hoàn toàn
            if (string.Equals(Model.PhanLoai, "App", StringComparison.OrdinalIgnoreCase))
                return;


            Pricing.ApplyCustomerPricingForAllLines(
                Model.ChiTietHoaDons,
                khId,
                AppProviders.KhachHangGiaBans.Items,
                showMessage ? (Action<string>)(msg => MessageBox.Show(msg, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information)) : null
            );
            ChiTietListBox.Items.Refresh();
            UpdateTotals();
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
                var selectedNotes = this.FindVisualChildren<RadioButton>()
                    .Where(r => r.IsChecked == true && r.GroupName != "LoaiDon")
                    .Select(r => r.Content.ToString())
                    .ToList();

                var noteTuDo = NoteTuDoTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(noteTuDo))
                    selectedNotes.Add(noteTuDo);

                selected.NoteText = selectedNotes.Any()
                    ? string.Join(" # ", selectedNotes)
                    : "";

                ChiTietListBox.Items.Refresh();
            }
        }

        private void DiemGroupBox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Model.DiemThangTruoc == -1)
            {
                MessageBox.Show("Khách hàng này không thuộc diện được nhận voucher.", "Thông báo");
                return;
            }

            if (Model.DaNhanVoucher)
            {
                MessageBox.Show("Khách hàng đã nhận voucher trong tháng này rồi.", "Thông báo");
                VoucherComboBox.IsEnabled = false;
                return;
            }

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

        private void ChiTietItem_Focus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ChiTietHoaDonDto ct)
                ChiTietListBox.SelectedItem = ct;
        }
        private void DonGiaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            if (tb.DataContext is not ChiTietHoaDonDto ct) return;

            if (decimal.TryParse(tb.Text.Replace(",", ""), out var newGia))
            {
                if (ct.DonGia != newGia)
                {
                    ct.DonGia = newGia;
                    UpdateTotals();
                }
            }
        }
        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ChiTietHoaDonDto ct) return;

            var list = Model.ChiTietHoaDons;
            int index = list.IndexOf(ct);
            if (index <= 0) return;

            list.Move(index, index - 1);
            Resequence();

            ChiTietListBox.Items.Refresh();
            FocusLine(ct);
        }
    }
}