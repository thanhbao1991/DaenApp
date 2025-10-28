using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using TraSuaApp.Shared.Config;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Services;
using TraSuaApp.WpfClient.AiOrdering;
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

        public Guid? SavedHoaDonId { get; internal set; }
        private readonly QuickOrderService _quick = new(Config.apiChatGptKey);
        private bool _openedFromMessenger;

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

        // 🟟 Đếm ngược 5 phút cố định (chỉ áp dụng khi thêm mới)
        private readonly System.Windows.Threading.DispatcherTimer _fixedTimer = new();
        private int _secondsLeft = 300; // 5 phút
        private bool _autoInvoked = false;
        private bool IsNewInvoice => Model?.Id == Guid.Empty;

        private bool _isSaving = false;
        private bool _isLoadingNote = false;

        #region Local helpers (no new files)
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
        #endregion

        // 🟟 Hiển thị mm:ss
        private void UpdateCountdownText()
        {
            var m = _secondsLeft / 60;
            var s = _secondsLeft % 60;
            AutoSaveCountdownText.Text = $"Tự lưu sau: {m:00}:{s:00}";
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
                    CreatedAt = DateTime.Now,
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
                if (Model.KhachHangId != null)
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

                ct.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ChiTietHoaDonDto.SoLuong) ||
                        e.PropertyName == nameof(ChiTietHoaDonDto.DonGia) ||
                        e.PropertyName == nameof(ChiTietHoaDonDto.ThanhTien) ||
                        e.PropertyName == nameof(ChiTietHoaDonDto.ToppingDtos))
                    {
                        UpdateTotals();
                    }
                };

                Model.ChiTietHoaDons.Add(ct);
                LoadToppingPanel(sanPham.NhomSanPhamId);

                // Binding đã lắng nghe thay đổi, không cần reset ItemsSource
                ChiTietListBox.SelectedItem = ct;
                ChiTietListBox.ScrollIntoView(ct);

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
                    var response = await ApiClient.GetAsync($"/api/Dashboard/thongtin-khachhang/{kh.Id}");
                    var info = await response.Content.ReadFromJsonAsync<KhachHangFavoriteDto>();

                    if (info != null)
                    {
                        Model.DiemThangNay = info.DiemThangNay;
                        Model.DiemThangTruoc = info.DiemThangTruoc;
                        Model.TongNoKhachHang = info.TongNo;
                        Model.DaNhanVoucher = info.DaNhanVoucher;

                        CongNoTextBlock.Text = info.TongNo.ToString("N0");
                        DiemThangNayTextBlock.Text = StarHelper.GetStarText(Model.DiemThangNay);
                        DiemThangTruocTextBlock.Text = StarHelper.GetStarText(Model.DiemThangTruoc);

                        if (info.DuocNhanVoucher && !info.DaNhanVoucher)
                        {
                            int saoDayTruoc = LoyaltyHelper.TinhSoSaoDay(Model.DiemThangTruoc);
                            int giaTriVoucher = LoyaltyHelper.TinhGiaTriVoucher(Model.DiemThangTruoc);

                            if (saoDayTruoc > 0 && giaTriVoucher > 0)
                            {
                                var blink = (Storyboard)FindResource("BlinkAnimation");
                                Storyboard.SetTarget(blink, DiemThangTruocGroupBox);
                                blink.Begin();
                                MessageBox.Show($"Voucher {giaTriVoucher.ToString("N0")}");
                            }
                        }

                        // Gợi ý món yêu thích
                        // SuggestFavoriteIntoSearchBoxByName(info.MonYeuThich);
                        RenderFavoriteChipsFromText(info?.MonYeuThich);
                        UpdateTotals();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi tải món hay order nhất: " + ex.Message);
                }

                // Cập nhật giá riêng cho các dòng đã có
                ApplyCustomerPricingForAllLines(kh.Id, showMessage: true);

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
                GoiYWrap.Children.Clear();
            };

            VoucherComboBox.ItemsSource = _voucherList;
            TenBanComboBox.ItemsSource = _banList;

            _api = new HoaDonApi();

            if (dto != null)
            {
                // ✅ Sửa hóa đơn
                Model = dto;

                foreach (var ct in Model.ChiTietHoaDons)
                {
                    ct.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ChiTietHoaDonDto.SoLuong) ||
                            e.PropertyName == nameof(ChiTietHoaDonDto.DonGia) ||
                            e.PropertyName == nameof(ChiTietHoaDonDto.ThanhTien) ||
                            e.PropertyName == nameof(ChiTietHoaDonDto.ToppingDtos))
                        {
                            UpdateTotals();
                        }
                    };
                }

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

            // 🟟 Đếm ngược cố định 5 phút, không reset theo thao tác
            _fixedTimer.Interval = TimeSpan.FromSeconds(1);
            _fixedTimer.Tick += FixedTimer_Tick;

            this.ContentRendered += (_, __) =>
            {
                if (IsNewInvoice)
                {
                    _secondsLeft = 300;
                    _autoInvoked = false;
                    AutoSaveCountdownText.Visibility = Visibility.Visible;
                    UpdateCountdownText();
                    _fixedTimer.Start();
                }
                else
                {
                    AutoSaveCountdownText.Visibility = Visibility.Collapsed;
                }
            };

            // === Watchers: tự đánh STT + tự tính tổng ===
            Model.ChiTietHoaDons.CollectionChanged += (_, __) =>
            {
                Resequence();
                UpdateTotals();
            };

            void AttachLineWatcher(ChiTietHoaDonDto ct)
            {
                ct.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(ChiTietHoaDonDto.SoLuong) ||
                        e.PropertyName == nameof(ChiTietHoaDonDto.DonGia) ||
                        e.PropertyName == nameof(ChiTietHoaDonDto.ThanhTien) ||
                        e.PropertyName == nameof(ChiTietHoaDonDto.ToppingDtos))
                    {
                        UpdateTotals();
                    }
                };
            }
            foreach (var ct in Model.ChiTietHoaDons) AttachLineWatcher(ct);
            Model.ChiTietHoaDons.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                    foreach (ChiTietHoaDonDto added in e.NewItems)
                        AttachLineWatcher(added);
            };
        }

        // 🟟 Không reset theo thao tác. Hết 5p -> gọi Save
        private void FixedTimer_Tick(object? sender, EventArgs e)
        {
            if (!IsNewInvoice)
            {
                _fixedTimer.Stop();
                AutoSaveCountdownText.Visibility = Visibility.Collapsed;
                return;
            }
            if (_isSaving) return;

            if (_secondsLeft > 0)
            {
                _secondsLeft--;
                UpdateCountdownText();
                return;
            }
            if (!_autoInvoked)
            {
                _autoInvoked = true;
                _fixedTimer.Stop();
                AutoSaveCountdownText.Text = "Đang tự lưu...";
                SaveButton_Click(SaveButton, new RoutedEventArgs());
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try { _fixedTimer.Stop(); } catch { }
            base.OnClosed(e);
        }

        /// <summary>
        /// Gợi ý "món yêu thích" vào ô tìm kiếm, không popup thừa.
        /// </summary>
        private void SuggestFavoriteIntoSearchBoxByName(string? favName)
        {
            try
            {
                if (_openedFromMessenger) return;
                if (string.IsNullOrWhiteSpace(favName)) return;
                if (Model.ChiTietHoaDons.Count != 0) return;  // chỉ gợi ý khi bill trống

                SanPhamSearchBox.SuppressPopup = false;
                SanPhamSearchBox.SearchTextBox.Text = favName;
                SanPhamSearchBox.SearchTextBox.Focus();
                SanPhamSearchBox.SearchTextBox.SelectAll();
                SanPhamSearchBox.IsPopupOpen = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SuggestFavoriteIntoSearchBoxByName error: " + ex.Message);
            }
        }

        private async Task RunGptFromMessengerIfNeededAsync(string latestCustomerName, string input)
        {
            try
            {
                if (!_openedFromMessenger) return;
                if (string.IsNullOrWhiteSpace(input)) return;

                bool isImage = false;
                if (File.Exists(input))
                {
                    var ext = Path.GetExtension(input).ToLowerInvariant();
                    isImage = ext == ".png" || ext == ".jpg" || ext == ".jpeg";
                }

                Guid? khId = null;
                if (KhachHangSearchBox.SelectedKhachHang is KhachHangDto kh)
                {
                    khId = kh.Id;
                }

                using (BusyUI.Scope(this, SaveButton, isImage ? "Đang phân tích ảnh..." : "Đang phân tích văn bản..."))
                {
                    var (hd, raw, preds) = await _quick.BuildHoaDonAsync(
                        input,
                        isImage: isImage,
                        khachHangId: khId,
                        customerNameHint: latestCustomerName
                    );

                    var parsed = hd ?? new HoaDonDto { ChiTietHoaDons = new() };
                    parsed.ChiTietHoaDons ??= new();

                    parsed.PhanLoai = string.IsNullOrWhiteSpace(Model.PhanLoai) ? "Ship" : Model.PhanLoai;

                    Model.ChiTietHoaDons.Clear();
                    foreach (var ct in parsed.ChiTietHoaDons)
                    {
                        ct.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(ChiTietHoaDonDto.SoLuong) ||
                                e.PropertyName == nameof(ChiTietHoaDonDto.DonGia) ||
                                e.PropertyName == nameof(ChiTietHoaDonDto.ThanhTien) ||
                                e.PropertyName == nameof(ChiTietHoaDonDto.ToppingDtos))
                            {
                                UpdateTotals();
                            }
                        };
                        Model.ChiTietHoaDons.Add(ct);
                    }

                    if (KhachHangSearchBox.SelectedKhachHang is KhachHangDto khSel)
                    {
                        ApplyCustomerPricingForAllLines(khSel.Id, showMessage: false);
                    }
                    else if (Model.KhachHangId != null)
                    {
                        ApplyCustomerPricingForAllLines(Model.KhachHangId.Value, showMessage: false);
                    }

                    Model.ChiTietHoaDonToppings = parsed.ChiTietHoaDonToppings;
                    Model.VoucherId = parsed.VoucherId;
                    Model.GhiChu = parsed.GhiChu;

                    ChiTietListBox.Items.Refresh();
                    UpdateTotals();

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
                }
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Mạng chậm/AI quá tải (timeout). Bạn có thể nhập tay tiếp.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("GPT lỗi: " + ex.Message);
                Debug.WriteLine(ex);
            }
        }

        public HoaDonEdit(HoaDonDto? dto, string? gptInput, string? latestCustomerName, bool openedFromMessenger)
            : this(dto) // gọi lại constructor gốc để khởi tạo UI/bindings
        {
            _openedFromMessenger = openedFromMessenger;

            if (!string.IsNullOrWhiteSpace(latestCustomerName))
                KhachHangSearchBox.SearchTextBox.Text = latestCustomerName;

            this.ContentRendered += async (_, __) => await RunGptFromMessengerIfNeededAsync(latestCustomerName, gptInput);
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
            decimal tongTien = 0;

            foreach (var ct in Model.ChiTietHoaDons)
            {
                decimal tienTopping = ct.ToppingDtos?.Sum(t => t.Gia * t.SoLuong) ?? 0;
                tongTien += (ct.DonGia * ct.SoLuong) + tienTopping;
            }

            var currVoucher = VoucherComboBox.SelectedItem as VoucherDto;
            decimal giamGia = Pricing.CalcVoucherDiscount(tongTien, currVoucher, Model.GiamGia);

            Model.TongTien = tongTien;
            Model.GiamGia = giamGia;
            Model.ThanhTien = tongTien - giamGia;

            TongTienTextBlock.Text = Model.TongTien.ToString("N0") + " đ";
            GiamGiaTextBlock.Text = Model.GiamGia.ToString("N0") + " đ";
            ThanhTienTextBlock.Text = Model.ThanhTien.ToString("N0") + " đ";

            TongSoSanPhamTextBlock.Text = Model.ChiTietHoaDons
                .Where(ct =>
                {
                    var bienThe = _bienTheList.FirstOrDefault(bt => bt.Id == ct.SanPhamIdBienThe);
                    if (bienThe == null) return false;

                    var sp = _sanPhamList.FirstOrDefault(s => s.Id == bienThe.SanPhamId);
                    if (sp == null) return false;

                    return sp.TenNhomSanPham != "Thuốc lá"
                        && sp.TenNhomSanPham != "Nước lon"
                        && sp.TenNhomSanPham != "Ăn vặt";
                })
                .Sum(ct => ct.SoLuong)
                .ToString("N0");
        }
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

            ChiTietListBox.SelectedItem = existing;
            ChiTietListBox.ScrollIntoView(existing);
            SanPhamSearchBox.SearchTextBox.Focus();
            SanPhamSearchBox.SearchTextBox.SelectAll();
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
                    SoLuong = 0
                })
                .ToList();

            ToppingListBox.ItemsSource = dsTopping;
        }

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
        // ====== FAVORITE QUICK CHIPS ======
        private void RenderFavoriteChipsFromText(string? raw)
        {
            try
            {
                GoiYWrap.Children.Clear();

                // Không có text => ẩn khối
                if (string.IsNullOrWhiteSpace(raw))
                {
                    return;
                }

                // Tách theo ; , / | xuống dòng
                var parts = raw
                    .Split(new[] { ';', ',', '/', '|', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (parts.Count == 0)
                {
                    return;
                }

                // Tạo button cho từng cụm chữ
                foreach (var name in parts)
                {
                    var sp = FindSanPhamByNameLoose(name);
                    if (sp == null) continue; // không map được thì bỏ

                    var btn = new Button
                    {
                        Style = Application.Current.FindResource("AddButtonStyle") as Style,
                        Margin = new Thickness(4),
                        Content = name,
                        Opacity = 1,
                        Tag = sp.Id
                    };
                    btn.Click += FavoriteChip_Click;
                    GoiYWrap.Children.Add(btn);
                }

            }
            catch { }
        }

        // Tìm sản phẩm theo tên "lỏng tay": ưu tiên trùng tuyệt đối, rồi startsWith, rồi contains (không phân biệt hoa thường)
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

        // Click 1 nút gợi ý -> thêm 1 dòng (biến thể rẻ nhất), áp giá riêng nếu có
        private void FavoriteChip_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Guid spId) return;
            var sp = _sanPhamList.FirstOrDefault(x => x.Id == spId);
            if (sp == null) return;

            var bt = sp.BienThe?.OrderBy(x => x.GiaBan).FirstOrDefault();
            if (bt == null) return;

            var ct = new ChiTietHoaDonDto
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.Now,
                LastModified = DateTime.Now,
                SanPhamIdBienThe = bt.Id,
                TenSanPham = sp.Ten,
                TenBienThe = bt.TenBienThe,
                SoLuong = 1,
                Stt = 0,
                BienTheList = _bienTheList.Where(x => x.SanPhamId == sp.Id).ToList(),
                ToppingDtos = new List<ToppingDto>()
            };

            decimal donGia = bt.GiaBan;
            if (Model.KhachHangId != null)
            {
                var customGia = AppProviders.KhachHangGiaBans.Items
                    .FirstOrDefault(x => x.KhachHangId == Model.KhachHangId.Value
                                      && x.SanPhamBienTheId == bt.Id
                                      && !x.IsDeleted);
                if (customGia != null) donGia = customGia.GiaBan;
            }
            ct.DonGia = donGia;

            ct.PropertyChanged += (s2, e2) =>
            {
                if (e2.PropertyName is nameof(ChiTietHoaDonDto.SoLuong)
                    or nameof(ChiTietHoaDonDto.DonGia)
                    or nameof(ChiTietHoaDonDto.ThanhTien)
                    or nameof(ChiTietHoaDonDto.ToppingDtos))
                {
                    UpdateTotals();
                }
            };

            Model.ChiTietHoaDons.Add(ct);
            ChiTietListBox.SelectedItem = ct;
            ChiTietListBox.ScrollIntoView(ct);
            UpdateTotals();

            // UX: quay lại ô search để nhập tiếp
            SanPhamSearchBox.SearchTextBox.Focus();
            SanPhamSearchBox.SearchTextBox.SelectAll();
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

        private void CapNhatChiTietSanPham(int soLuong, bool forceNewLine = false)
        {
            string currentNote = string.Join(" # ",
                this.FindVisualChildren<RadioButton>()
                    .Where(r => r.IsChecked == true && r.GroupName != "LoaiDon")
                    .Select(r => r.Content?.ToString() ?? "")
            );

            ChiTietHoaDonDto? existing = null;

            if (!forceNewLine)
                existing = ChiTietListBox.SelectedItem as ChiTietHoaDonDto;

            if (soLuong == 0)
            {
                if (existing != null)
                {
                    Model.ChiTietHoaDons.Remove(existing);
                    ChiTietListBox.Items.Refresh();
                    UpdateTotals();
                }
            }
            else
            {
                if (existing == null)
                {
                    if (SanPhamSearchBox.SelectedSanPham is SanPhamDto sanPham &&
                        SanPhamSearchBox.SelectedBienThe is SanPhamBienTheDto bienThe)
                    {
                        existing = new ChiTietHoaDonDto
                        {
                            Id = Guid.NewGuid(),
                            SanPhamIdBienThe = bienThe.Id,
                            TenSanPham = sanPham.Ten,
                            TenBienThe = bienThe.TenBienThe,
                            SoLuong = soLuong,
                            BienTheList = _bienTheList.Where(bt => bt.SanPhamId == sanPham.Id).ToList(),
                            ToppingDtos = new List<ToppingDto>(),
                            NoteText = currentNote
                        };

                        decimal donGia = bienThe.GiaBan;
                        if (Model.KhachHangId != null)
                        {
                            var customGia = AppProviders.KhachHangGiaBans.Items
                                .FirstOrDefault(x => x.KhachHangId == Model.KhachHangId.Value
                                                  && x.SanPhamBienTheId == bienThe.Id
                                                  && !x.IsDeleted);
                            if (customGia != null)
                                donGia = customGia.GiaBan;
                        }
                        existing.DonGia = donGia;

                        Model.ChiTietHoaDons.Add(existing);
                        CapNhatToppingChoSanPham();
                    }
                }
                else
                {
                    existing.SoLuong = soLuong;
                    existing.NoteText = currentNote;
                }

                Resequence();
                ChiTietListBox.Items.Refresh();
                UpdateTotals();

                if (existing != null)
                {
                    ChiTietListBox.SelectedItem = existing;
                    ChiTietListBox.ScrollIntoView(existing);
                }

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
            Pricing.ApplyCustomerPricingForAllLines(
                Model.ChiTietHoaDons,
                khId,
                AppProviders.KhachHangGiaBans.Items,
                showMessage ? (Action<string>)(msg => MessageBox.Show(msg, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information)) : null
            );
            ChiTietListBox.Items.Refresh();
            UpdateTotals();
        }

        private void ThemDongButton_Click(object sender, RoutedEventArgs e)
        {
            if (ChiTietListBox.SelectedItem is not ChiTietHoaDonDto)
            {
                MessageBox.Show("Vui lòng chọn món để thêm dòng mới.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            CapNhatChiTietSanPham(1, true);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) { }

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

        private void DiemThangTruocTextBlock_MouseDown(object sender, MouseButtonEventArgs e)
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

        private void DonGiaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is ChiTietHoaDonDto ct)
            {
                if (decimal.TryParse(tb.Text, out var newGia))
                {
                    if (ct.DonGia != newGia)
                    {
                        ct.DonGia = newGia;
                        UpdateTotals();
                    }
                }
            }
        }

        private void ChiTietItem_Focus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ChiTietHoaDonDto ct)
                ChiTietListBox.SelectedItem = ct;
        }

        private void TangSoLuong_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ChiTietHoaDonDto ct)
            {
                ct.SoLuong++;
                ChiTietListBox.Items.Refresh();
                UpdateTotals();
            }
        }

        private void GiamSoLuong_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ChiTietHoaDonDto ct)
            {
                if (ct.SoLuong > 0) ct.SoLuong--;
                ChiTietListBox.Items.Refresh();
                UpdateTotals();
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
            ChiTietListBox.SelectedItem = ct;
            ChiTietListBox.ScrollIntoView(ct);
        }
        // HoaDonEdit.xaml.cs
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving) return;
            _isSaving = true;

            try
            {
                // ------------------ UI lock tối thiểu ------------------
                SaveButton.IsEnabled = false;
                NoiDungForm.IsEnabled = false;

                // ------------------ VALIDATION GỐC (GIỮ NGUYÊN) ------------------
                if (TaiChoRadio.IsChecked == true) Model.PhanLoai = "Tại Chỗ";
                else if (MuaVeRadio.IsChecked == true) Model.PhanLoai = "Mv";
                else if (ShipRadio.IsChecked == true) Model.PhanLoai = "Ship";
                else if (AppRadio.IsChecked == true) Model.PhanLoai = "App";

                Model.TrangThai = "";
                Model.TenBan = TenBanComboBox.Text;
                Model.KhachHangId = KhachHangSearchBox.SelectedKhachHang?.Id;
                Model.TenKhachHangText = KhachHangSearchBox.SearchTextBox.Text?.Trim();
                Model.SoDienThoaiText = DienThoaiComboBox.Text?.Trim();
                Model.DiaChiText = DiaChiComboBox.Text?.Trim();
                Model.VoucherId = (Guid?)VoucherComboBox.SelectedValue;

                Model.ChiTietHoaDonVouchers = new List<ChiTietHoaDonVoucherDto>();
                if (VoucherComboBox.SelectedItem is VoucherDto voucher)
                {
                    if (voucher.Id != Guid.Empty)
                    {
                        Model.ChiTietHoaDonVouchers.Add(new ChiTietHoaDonVoucherDto
                        {
                            VoucherId = voucher.Id,
                            GiaTriApDung = voucher.GiaTri
                        });
                    }
                }

                // Validation Ship
                if (Model.PhanLoai == "Ship")
                {
                    if (string.IsNullOrWhiteSpace(Model.TenKhachHangText))
                    { ErrorTextBlock.Text = "Tên khách hàng không được để trống."; return; }
                    if (string.IsNullOrWhiteSpace(Model.DiaChiText))
                    { ErrorTextBlock.Text = "Địa chỉ không được để trống."; return; }
                    if (string.IsNullOrWhiteSpace(Model.SoDienThoaiText))
                    { ErrorTextBlock.Text = "SĐT không được để trống."; return; }
                }

                // Validation bàn
                if (TaiChoRadio.IsChecked == true && string.IsNullOrWhiteSpace(Model.TenBan))
                {
                    ErrorTextBlock.Text = "Tên không được để trống.";
                    TenBanComboBox.IsDropDownOpen = true;
                    return;
                }

                if (Model.ChiTietHoaDons.Count == 0 || !Model.ChiTietHoaDons.Any(ct => ct.SoLuong > 0))
                {
                    ErrorTextBlock.Text = "Chưa có sản phẩm nào trong hóa đơn.";
                    return;
                }

                // Đồng bộ địa chỉ + SĐT mới vào Model như code cũ
                if (KhachHangSearchBox.SelectedKhachHang is KhachHangDto kh)
                {
                    // Địa chỉ
                    var diaChiText = Model.DiaChiText ?? "";
                    if (!string.IsNullOrWhiteSpace(diaChiText) &&
                        !kh.Addresses.Any(a => a.DiaChi.Equals(diaChiText, StringComparison.OrdinalIgnoreCase)))
                    {
                        var d = new KhachHangAddressDto { Id = Guid.NewGuid(), DiaChi = diaChiText };
                        kh.Addresses.Add(d);
                    }

                    // SĐT
                    var sdt = Model.SoDienThoaiText ?? "";
                    if (!string.IsNullOrWhiteSpace(sdt) &&
                        !kh.Phones.Any(p => p.SoDienThoai.Equals(sdt, StringComparison.OrdinalIgnoreCase)))
                    {
                        var ph = new KhachHangPhoneDto { Id = Guid.NewGuid(), SoDienThoai = sdt };
                        kh.Phones.Add(ph);
                    }
                }

                // ================== LOCAL FIRST ==================
                bool isNew = Model.Id == Guid.Empty;
                if (isNew)
                    Model.Id = Guid.NewGuid();

                // Lọc dòng hợp lệ
                Model.ChiTietHoaDons = new ObservableCollection<ChiTietHoaDonDto>(
                    Model.ChiTietHoaDons.Where(ct => ct.SoLuong > 0)
                );

                DongBoTatCaTopping(); // đồng bộ đúng tổng

                // ✅ Trả kết quả ngay cho UI ngoài
                SavedHoaDonId = Model.Id;
                DialogResult = true;
                Close();

                // ------------------ API ngầm ------------------
                _ = Task.Run(async () =>
                {
                    try
                    {
                        Result<HoaDonDto> result;
                        if (isNew)
                        {
                            result = await _api.CreateAsync(Model);
                            if (result.IsSuccess && result.Data?.KhachHangId != null)
                                await AppProviders.KhachHangs.ReloadAsync();
                        }
                        else if (Model.IsDeleted)
                        {
                            result = await _api.RestoreAsync(Model.Id);
                        }
                        else
                        {
                            result = await _api.UpdateAsync(Model.Id, Model);
                        }

                        if (!result.IsSuccess)
                        {
                            NotiHelper.ShowError(result.Message);
                            return;
                        }

                        await AppProviders.HoaDons.ReloadAsync();
                    }
                    catch (Exception ex)
                    {
                        NotiHelper.ShowError("Lỗi đường truyền khi lưu hóa đơn: " + ex.Message);
                    }
                });

                // ------------------ Log nếu có Messenger ------------------
                if (_openedFromMessenger && Model?.ChiTietHoaDons?.Any() == true)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var lines = new List<string>();
                            foreach (var ct in Model.ChiTietHoaDons.OrderBy(x => x.Stt))
                                lines.Add($"{ct.Stt}. {ct.TenSanPham} x{ct.SoLuong}");
                            await DiscordService.SendAsync(DiscordEventType.Admin, string.Join("\n", lines));
                        }
                        catch { }
                    });
                }
            }
            catch (Exception ex)
            {
                ErrorTextBlock.Text = ex.Message;
            }
            finally
            {
                _isSaving = false;
                try
                {
                    SaveButton.IsEnabled = true;
                    NoiDungForm.IsEnabled = true;
                }
                catch { }
            }
        }
        private void DongBoTatCaTopping()
        {
            if (Model.ChiTietHoaDonToppings == null)
                Model.ChiTietHoaDonToppings = new List<ChiTietHoaDonToppingDto>();

            ToppingSync.SyncAll(Model.ChiTietHoaDons, Model.ChiTietHoaDonToppings);
            UpdateTotals();
        }
    }
}