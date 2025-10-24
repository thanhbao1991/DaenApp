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
                    //DonGia = bienThe.GiaBan,
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

                        // 🟟 Thông báo ngay khi thêm món
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
                        e.PropertyName == nameof(ChiTietHoaDonDto.ThanhTien))
                    {
                        CapNhatTongTien();
                    }
                };


                Model.ChiTietHoaDons.Add(ct);

                // đánh lại STT
                int stt = 1;
                foreach (var item in Model.ChiTietHoaDons)
                {
                    item.Stt = stt++;
                }
                LoadToppingPanel(sanPham.NhomSanPhamId);
                CapNhatTongTien();

                ChiTietListBox.ItemsSource = null;
                ChiTietListBox.ItemsSource = Model.ChiTietHoaDons;
                ChiTietListBox.SelectedItem = ct;
                ChiTietListBox.ScrollIntoView(ct);

                SanPhamSearchBox.SearchTextBox.Focus();
                SanPhamSearchBox.SearchTextBox.SelectAll();
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
                VoucherComboBox.IsEnabled = true;

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
                                MessageBox.Show($"Voucher {giaTriVoucher.ToString("N0")}");
                            }
                        }

                        ChiTietListBox.ItemsSource = null;
                        ChiTietListBox.ItemsSource = Model.ChiTietHoaDons;

                        // 🟟 Gợi ý món yêu thích theo ID (không phụ thuộc tên)
                        SuggestFavoriteIntoSearchBoxByName(info.MonYeuThich);

                        CapNhatTongTien();
                    }

                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Lỗi tải món hay order nhất: " + ex.Message);
                }

                // 🟟 Cập nhật giá riêng theo khách hàng cho các món đã chọn
                var dsMonCapNhat = new List<string>();

                foreach (var ct in Model.ChiTietHoaDons)
                {
                    var customGia = AppProviders.KhachHangGiaBans.Items
                        .FirstOrDefault(x => x.KhachHangId == kh.Id
                                          && x.SanPhamBienTheId == ct.SanPhamIdBienThe
                                          && !x.IsDeleted);
                    if (customGia != null)
                    {
                        ct.DonGia = customGia.GiaBan;
                        dsMonCapNhat.Add($"{ct.TenSanPham} ({ct.DonGia})");
                    }
                }

                // Nếu có món được cập nhật → thông báo chi tiết
                if (dsMonCapNhat.Any())
                {
                    string msg = "Đã cập nhật giá riêng cho các món:\n- "
                               + string.Join("\n- ", dsMonCapNhat);

                    MessageBox.Show(
                        msg,
                        "Thông báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }

                // Refresh lại UI và tổng tiền
                ChiTietListBox.ItemsSource = null;
                ChiTietListBox.ItemsSource = Model.ChiTietHoaDons;
                CapNhatTongTien();

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


                foreach (var ct in Model.ChiTietHoaDons)
                {
                    var bienThe = _bienTheList.FirstOrDefault(bt => bt.Id == ct.SanPhamIdBienThe);

                    ct.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(ChiTietHoaDonDto.SoLuong) ||
                            e.PropertyName == nameof(ChiTietHoaDonDto.DonGia) ||
                            e.PropertyName == nameof(ChiTietHoaDonDto.ThanhTien))
                        {
                            CapNhatTongTien();
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
            CapNhatTongTien();

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

            if (_isSaving) return; // đang lưu tay thì thôi cứ hiện số cũ

            if (_secondsLeft > 0)
            {
                _secondsLeft--;
                UpdateCountdownText();
            }

            if (_secondsLeft <= 0 && !_autoInvoked)
            {
                _autoInvoked = true;
                _fixedTimer.Stop();
                AutoSaveCountdownText.Text = "Đang tự lưu...";
                // Gọi Save như bấm nút (giữ nguyên validation của bạn)
                SaveButton_Click(SaveButton, new RoutedEventArgs());
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            try { _fixedTimer.Stop(); } catch { }
            base.OnClosed(e);
        }
        /// <summary>
        /// Đưa "món yêu thích" vào ô tìm kiếm sản phẩm và mở popup để user chọn.
        /// - Không hiển thị MessageBox.
        /// - Bôi đen text để user xoá nhanh nếu không cần.
        /// - Mặc định chỉ gợi ý khi bill đang trống (có thể bỏ điều kiện nếu muốn).
        /// </summary>
        private void SuggestFavoriteIntoSearchBoxByName(string? favName)
        {
            try
            {
                if (_openedFromMessenger) return;             // không gợi ý khi mở từ Messenger
                if (string.IsNullOrWhiteSpace(favName)) return;
                if (Model.ChiTietHoaDons.Count != 0) return;  // chỉ gợi ý khi bill trống (tuỳ bạn)

                // Điền text, focus, select-all để user xoá nhanh nếu không cần
                SanPhamSearchBox.SuppressPopup = false;
                SanPhamSearchBox.SearchTextBox.Text = favName;
                SanPhamSearchBox.SearchTextBox.Focus();
                SanPhamSearchBox.SearchTextBox.SelectAll();

                // Mở popup để người dùng bấm chọn biến thể → event SanPhamBienTheSelected của bạn sẽ tự add dòng
                SanPhamSearchBox.IsPopupOpen = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("SuggestFavoriteIntoSearchBoxByName error: " + ex.Message);
            }
        }
        private async Task RunGptFromMessengerIfNeededAsync(string latestCustomerName, string input)
        {
            try
            {
                // Chỉ chạy khi mở từ Messenger và có truyền chuỗi
                if (!_openedFromMessenger) return;
                if (string.IsNullOrWhiteSpace(input)) return;

                // Xác định kiểu input: TEXT hay ẢNH
                bool isImage = false;
                if (File.Exists(input))
                {
                    var ext = Path.GetExtension(input).ToLowerInvariant();
                    isImage = ext == ".png" || ext == ".jpg" || ext == ".jpeg";
                }

                // Lấy lịch sử/”short menu” theo khách (nếu đã chọn)
                Guid? khId = null;
                if (KhachHangSearchBox.SelectedKhachHang is KhachHangDto kh)
                {
                    khId = kh.Id;
                }

                using (BusyUI.Scope(this, SaveButton, isImage ? "Đang phân tích ảnh..." : "Đang phân tích văn bản..."))
                {
                    // Gọi AI ngay tại form
                    var (hd, raw, preds) = await _quick.BuildHoaDonAsync(
                        input,
                        isImage: isImage,
                        khachHangId: khId,
    customerNameHint: latestCustomerName    // ✅ giúp bỏ dòng "Mun"
);


                    // Nếu AI không nhận ra gì vẫn mở đơn rỗng để nhập tay
                    var parsed = hd ?? new HoaDonDto { ChiTietHoaDons = new() };
                    parsed.ChiTietHoaDons ??= new();

                    // Giữ loại đơn hiện tại (mặc định Ship)
                    parsed.PhanLoai = string.IsNullOrWhiteSpace(Model.PhanLoai) ? "Ship" : Model.PhanLoai;

                    // Áp kết quả vào UI hiện tại
                    // 1) thay danh sách chi tiết
                    Model.ChiTietHoaDons.Clear();
                    foreach (var ct in parsed.ChiTietHoaDons)
                    {
                        // gắn lắng nghe để tự tính tiền khi sửa số lượng/đơn giá
                        ct.PropertyChanged += (s, e) =>
                        {
                            if (e.PropertyName == nameof(ChiTietHoaDonDto.SoLuong) ||
                                e.PropertyName == nameof(ChiTietHoaDonDto.DonGia) ||
                                e.PropertyName == nameof(ChiTietHoaDonDto.ThanhTien))
                            {
                                CapNhatTongTien();
                            }
                        };
                        Model.ChiTietHoaDons.Add(ct);
                    }
                    // ✅ Nếu đã chọn KH từ trước, áp lại giá riêng cho toàn bộ dòng GPT vừa đổ
                    if (KhachHangSearchBox.SelectedKhachHang is KhachHangDto khSel)
                    {
                        ApplyCustomerPricingForAllLines(khSel.Id, showMessage: false); // im lặng, tránh popup trùng
                    }
                    else if (Model.KhachHangId != null)
                    {
                        ApplyCustomerPricingForAllLines(Model.KhachHangId.Value, showMessage: false);
                    }
                    // 2) topping, voucher, KH, ghi chú...
                    Model.ChiTietHoaDonToppings = parsed.ChiTietHoaDonToppings;
                    Model.VoucherId = parsed.VoucherId;
                    Model.GhiChu = parsed.GhiChu;

                    // 3) cập nhật các control phụ
                    ChiTietListBox.ItemsSource = Model.ChiTietHoaDons;
                    ChiTietListBox.Items.Refresh();
                    CapNhatTongTien();

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

            // Gợi ý sẵn tên khách lấy từ Messenger (chỉ set text, chưa auto chọn)
            if (!string.IsNullOrWhiteSpace(latestCustomerName))
                KhachHangSearchBox.SearchTextBox.Text = latestCustomerName;

            // Khi UI hiển thị xong mới chạy GPT (nếu đủ điều kiện)
            this.ContentRendered += async (_, __) => await RunGptFromMessengerIfNeededAsync(latestCustomerName, gptInput);
        }
        private bool _isSaving = false;
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isSaving) return;
            _isSaving = true;
            SaveButton.IsEnabled = false;
            NoiDungForm.IsEnabled = false;
            using (BusyUI.Scope(this, SaveButton, "Đang lưu..."))
            {

                try
                {
                    if (TaiChoRadio.IsChecked == true)
                        Model.PhanLoai = "Tại Chỗ";
                    else if (MuaVeRadio.IsChecked == true)
                        Model.PhanLoai = "Mv";
                    else if (ShipRadio.IsChecked == true)
                        Model.PhanLoai = "Ship";
                    else if (AppRadio.IsChecked == true)
                        Model.PhanLoai = "App";

                    Model.TrangThai = "";
                    Model.TenBan = TenBanComboBox.Text;
                    Model.KhachHangId = KhachHangSearchBox.SelectedKhachHang?.Id;

                    // Luôn lấy text đang hiển thị
                    Model.TenKhachHangText = KhachHangSearchBox.SearchTextBox.Text?.Trim();
                    Model.SoDienThoaiText = DienThoaiComboBox.Text?.Trim();
                    Model.DiaChiText = DiaChiComboBox.Text?.Trim();

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

                    if (Model.PhanLoai == "Ship")
                    {
                        if (string.IsNullOrWhiteSpace(Model.TenKhachHangText))
                        {
                            ErrorTextBlock.Text = $"Tên khách hàng không được để trống.";
                            KhachHangSearchBox.SearchTextBox.Focus();
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(Model.DiaChiText))
                        {
                            ErrorTextBlock.Text = $"Địa chỉ không được để trống.";
                            DiaChiComboBox.Focus();
                            return;
                        }
                        if (string.IsNullOrWhiteSpace(Model.SoDienThoaiText))
                        {
                            ErrorTextBlock.Text = $"SĐT không được để trống.";
                            DienThoaiComboBox.Focus();
                            return;
                        }
                    }
                    if (TaiChoRadio.IsChecked == true && string.IsNullOrWhiteSpace(Model.TenBan))
                    {
                        ErrorTextBlock.Text = "Tên không được để trống.";
                        TenBanComboBox.IsDropDownOpen = true;
                        return;
                    }
                    if (Model.ChiTietHoaDons.Count == 0)
                    {
                        ErrorTextBlock.Text = "Chưa có sản phẩm nào trong hóa đơn.";
                        return;
                    }

                    // Thêm địa chỉ/điện thoại mới vào KH nếu cần (giữ code cũ của bạn)
                    if (KhachHangSearchBox.SelectedKhachHang is KhachHangDto kh)
                    {
                        string diaChiText = DiaChiComboBox.Text?.Trim() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(diaChiText) && !kh.Addresses.Any(a => a.DiaChi.Equals(diaChiText, StringComparison.OrdinalIgnoreCase)))
                        {
                            var diaChiMoi = new KhachHangAddressDto { Id = Guid.NewGuid(), DiaChi = diaChiText, IsDefault = false };
                            kh.Addresses.Add(diaChiMoi);
                            DiaChiComboBox.ItemsSource = null;
                            DiaChiComboBox.ItemsSource = kh.Addresses;
                            DiaChiComboBox.SelectedItem = diaChiMoi;
                        }

                        string sdtText = DienThoaiComboBox.Text?.Trim() ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(sdtText) && !kh.Phones.Any(p => p.SoDienThoai.Equals(sdtText, StringComparison.OrdinalIgnoreCase)))
                        {
                            var sdtMoi = new KhachHangPhoneDto { Id = Guid.NewGuid(), SoDienThoai = sdtText, IsDefault = false };
                            kh.Phones.Add(sdtMoi);
                            DienThoaiComboBox.ItemsSource = null;
                            DienThoaiComboBox.ItemsSource = kh.Phones;
                            DienThoaiComboBox.SelectedItem = sdtMoi;
                        }
                    }

                    if (!Model.ChiTietHoaDons.Any(ct => ct.SoLuong > 0))
                    {
                        ErrorTextBlock.Text = "Chưa có sản phẩm nào trong hóa đơn.";
                        return;
                    }

                    // Chỉ giữ dòng > 0
                    Model.ChiTietHoaDons = new ObservableCollection<ChiTietHoaDonDto>(
                        Model.ChiTietHoaDons.Where(ct => ct.SoLuong > 0)
                    );

                    // Đồng bộ topping & tính tiền
                    DongBoTatCaTopping();

                    // == GỌI API ==
                    bool isNew = Model.Id == Guid.Empty;
                    if (isNew) Model.Id = Guid.NewGuid();

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
                        ErrorTextBlock.Text = result.Message;
                        return;
                    }

                    SavedHoaDonId = Model.Id != Guid.Empty ? Model.Id : null;

                    // đóng cửa sổ và trả kết quả OK
                    DialogResult = true;
                    this.DialogResult = true;
                    this.Close();
                }
                catch (Exception ex)
                {
                    ErrorTextBlock.Text = ex.Message;
                }
                finally
                {
                    // Nếu chưa đóng thì khôi phục (nếu đã Close, UI sẽ bị dispose; try-catch để an toàn)
                    try
                    {
                        SaveButton.IsEnabled = true;
                        NoiDungForm.IsEnabled = true;
                    }
                    catch { }
                    _isSaving = false;
                }

            }




            // 🟟 Chỉ log khi mở từ Messenger
            if (_openedFromMessenger && Model?.ChiTietHoaDons?.Any() == true)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var lines = new List<string>();
                        lines.Add("----- USER ORDER FINALIZED -----");

                        foreach (var ct in Model.ChiTietHoaDons.OrderBy(x => x.Stt))
                        {
                            var bienThe = AppProviders.SanPhams.Items
                                .SelectMany(s => s.BienThe)
                                .FirstOrDefault(bt => bt.Id == ct.SanPhamIdBienThe);

                            string tenBienThe = bienThe?.TenBienThe;
                            string tenHienThi = ct.TenSanPham + $" – {tenBienThe}";

                            string noteText = string.IsNullOrWhiteSpace(ct.NoteText) ? "" : $" - {ct.NoteText}";
                            string toppingText = "";
                            if (ct.ToppingDtos?.Any() == true)
                                toppingText = "  + " + string.Join(", ", ct.ToppingDtos.Select(t => $"{t.Ten} x{t.SoLuong}"));

                            string gia = $"{ct.DonGia:N0}đ";
                            lines.Add($"{ct.Stt}. {tenHienThi} x{ct.SoLuong} - {ct.DonGia:N0}đ - {noteText}");
                        }

                        await DiscordService.SendAsync(
                            Shared.Enums.DiscordEventType.Admin,
                            string.Join("\n", lines)
                        );
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Lỗi gửi log Discord GPT vs USER: " + ex.Message);
                    }
                });
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseButton_Click(null!, null!);
                return;
            }
            else
            if (e.Key == Key.Enter)
            {
                if (KhachHangSearchBox.IsPopupOpen || SanPhamSearchBox.IsPopupOpen) return;
                if (_isSaving) return;       // ⬅️ đang lưu thì bỏ
                e.Handled = true;
                SaveButton_Click(SaveButton, new RoutedEventArgs());
            }
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
                        Ten = t.Ten,

                    });
                }
            }
        }

        private void ResetSanPhamInputs()
        {
            _isLoadingNote = true;               // ⬅️ chặn cập nhật note khi reset

            try
            {
                // Clear tất cả radio (trừ nhóm LoaiDon)
                foreach (var radio in this.FindVisualChildren<RadioButton>().Where(r => r.GroupName != "LoaiDon"))
                    radio.IsChecked = false;

                // Tuỳ bạn: nếu muốn TextBox ghi chú tự do cũng reset về rỗng
                NoteTuDoTextBox.Text = string.Empty;

                // Reset topping
                if (ToppingListBox.ItemsSource is List<ToppingDto> ds)
                {
                    foreach (var t in ds) t.SoLuong = 0;
                    ToppingListBox.Items.Refresh();
                }
            }
            finally
            {
                _isLoadingNote = false;          // mở lại cập nhật note
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


            // 🟟 Chỉ tính tổng số lượng sản phẩm KHÔNG thuộc nhóm "Thuốc lá" và "Ăn vặt"
            TongSoSanPhamTextBlock.Text = Model.ChiTietHoaDons
                .Where(ct =>
                {
                    var bienThe = _bienTheList.FirstOrDefault(bt => bt.Id == ct.SanPhamIdBienThe);
                    if (bienThe == null) return false;

                    var sp = _sanPhamList.FirstOrDefault(s => s.Id == bienThe.SanPhamId);
                    if (sp == null) return false;

                    return
                    sp.TenNhomSanPham != "Thuốc lá"
                    && sp.TenNhomSanPham != "Nước lon"
                    && sp.TenNhomSanPham != "Ăn vặt";
                })
                .Sum(ct => ct.SoLuong)
                .ToString("N0");
        }
        private void CapNhatToppingChoSanPham()
        {
            // lấy dòng chi tiết hiện tại
            var existing = ChiTietListBox.SelectedItem as ChiTietHoaDonDto
                           ?? Model.ChiTietHoaDons.LastOrDefault();

            if (existing == null) return;

            existing.ToppingDtos.Clear();

            // Xóa topping cũ trong Model.ChiTietHoaDonToppings
            foreach (var item in Model.ChiTietHoaDonToppings
                         .Where(tp => tp.ChiTietHoaDonId == existing.Id).ToList())
            {
                Model.ChiTietHoaDonToppings.Remove(item);
            }

            // Đọc topping từ ListBox hiển thị
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

            // Cập nhật text hiển thị topping
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

            //if (MessageBox.Show($"Xoá {ct.TenSanPham}?", "Xác nhận", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            //    return;

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
                //else
                //{
                //    VoucherComboBox.SelectedIndex = -1;
                //}

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



        private void CapNhatChiTietSanPham(int soLuong, bool forceNewLine = false)
        {
            string currentNote = string.Join(" # ",
         this.FindVisualChildren<RadioButton>()
             .Where(r => r.IsChecked == true && r.GroupName != "LoaiDon")
             .Select(r => r.Content?.ToString() ?? "")
     );

            ChiTietHoaDonDto? existing = null;

            if (!forceNewLine)
            {
                existing = ChiTietListBox.SelectedItem as ChiTietHoaDonDto;
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
                    // 🟟 Lấy từ sản phẩm đang chọn trong SearchBox
                    if (SanPhamSearchBox.SelectedSanPham is SanPhamDto sanPham &&
                        SanPhamSearchBox.SelectedBienThe is SanPhamBienTheDto bienThe)
                    {
                        existing = new ChiTietHoaDonDto
                        {
                            Id = Guid.NewGuid(),
                            SanPhamIdBienThe = bienThe.Id,
                            TenSanPham = sanPham.Ten,
                            TenBienThe = bienThe.TenBienThe,
                            // DonGia = bienThe.GiaBan, // set khi tạo mới
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
                            {
                                donGia = customGia.GiaBan;
                            }
                        }
                        existing.DonGia = donGia;



                        Model.ChiTietHoaDons.Add(existing);
                        CapNhatToppingChoSanPham();
                    }
                }
                else
                {
                    // 🟟 Cập nhật số lượng + note
                    existing.SoLuong = soLuong;
                    existing.NoteText = currentNote;
                }

                // Đánh lại STT
                int stt = 1;
                foreach (var ct in Model.ChiTietHoaDons)
                {
                    ct.Stt = stt++;
                }

                ChiTietListBox.Items.Refresh();
                CapNhatTongTien();

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


        }

        #region Giá riêng theo khách hàng
        private void ApplyCustomerPricingForAllLines(Guid khId, bool showMessage = true)
        {
            if (Model?.ChiTietHoaDons == null) return;

            var dsMonCapNhat = new List<string>();

            foreach (var ct in Model.ChiTietHoaDons)
            {
                var customGia = AppProviders.KhachHangGiaBans.Items
                    .FirstOrDefault(x => x.KhachHangId == khId
                                      && x.SanPhamBienTheId == ct.SanPhamIdBienThe
                                      && !x.IsDeleted);
                if (customGia != null && ct.DonGia != customGia.GiaBan)
                {
                    ct.DonGia = customGia.GiaBan;
                    dsMonCapNhat.Add($"{ct.TenSanPham} ({ct.DonGia:N0})");
                }
            }

            // Thông báo gộp (tránh spam khi nhiều món)
            if (showMessage && dsMonCapNhat.Any())
            {
                string msg = "Đã cập nhật giá riêng cho các món:\n- " + string.Join("\n- ", dsMonCapNhat);
                MessageBox.Show(msg, "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            // Refresh UI & tổng
            ChiTietListBox.ItemsSource = null;
            ChiTietListBox.ItemsSource = Model.ChiTietHoaDons;
            CapNhatTongTien();
        }
        #endregion
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
        private void DonGiaTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is ChiTietHoaDonDto ct)
            {
                if (decimal.TryParse(tb.Text, out var newGia))
                {
                    if (ct.DonGia != newGia)
                    {
                        ct.DonGia = newGia;
                        CapNhatTongTien();
                    }
                }
            }
        }
        private void ChiTietItem_Focus(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is ChiTietHoaDonDto ct)
            {
                ChiTietListBox.SelectedItem = ct;
            }
        }

        private void TangSoLuong_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ChiTietHoaDonDto ct)
            {
                ct.SoLuong++;
                ChiTietListBox.Items.Refresh();
                CapNhatTongTien();
            }
        }

        private void GiamSoLuong_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ChiTietHoaDonDto ct)
            {
                if (ct.SoLuong > 0) ct.SoLuong--;
                ChiTietListBox.Items.Refresh();
                CapNhatTongTien();
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ChiTietHoaDonDto ct) return;

            var list = Model.ChiTietHoaDons;
            int index = list.IndexOf(ct);
            if (index <= 0) return; // Đầu danh sách rồi

            // Di chuyển món lên trên
            list.Move(index, index - 1);

            // Đánh lại STT
            int stt = 1;
            foreach (var item in list)
                item.Stt = stt++;

            // Cập nhật UI
            ChiTietListBox.Items.Refresh();
            ChiTietListBox.SelectedItem = ct;
            ChiTietListBox.ScrollIntoView(ct);
        }
    }
}

