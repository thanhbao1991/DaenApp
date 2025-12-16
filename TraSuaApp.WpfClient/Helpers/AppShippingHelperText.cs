using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Services;

public class AppShippingHelperText
{
    private readonly string _username;
    private readonly string _password;
    private readonly string _cookieFile = "cookies.json";

    private readonly List<SanPhamDto> _sanPhamList;
    private readonly List<SanPhamBienTheDto> _bienTheList;
    private readonly List<ToppingDto> _toppingList;

    // --- Chrome driver singleton ---
    private static ChromeDriver? _driver;
    private static WebDriverWait? _wait;
    private PerfCdpSniffer? _sniffer;

    // Đánh dấu đã xác thực (đã login thành công ít nhất 1 lần)
    private static bool _isAuthenticated;

    // --- XPath & URL ---
    private readonly string usernameXPath = "//*[@id='app']/div/form/div[2]/div/div[1]/input";
    private readonly string passwordXPath = "//*[@id='app']/div/form/div[3]/div/div[1]/input";
    private readonly string loginButtonXPath = "/html/body/div/div/form/button";
    private readonly string avatarXPath = "//*[@id=\"app\"]/div/div[2]/div/div/div[3]/div/div/img";
    private readonly string orderPageUrl = "https://store.shippershipping.com/#/store/order";
    private readonly string xemChiTietRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[5]/div/div[1]/a/span";
    private readonly string chiTietPopupXPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[6]/div";

    // ⚠️ Constructor
    public AppShippingHelperText(string username, string password,
                                 List<SanPhamDto> sanPhamList,
                                 List<ToppingDto> toppingList)
    {
        _username = username;
        _password = password;

        _sanPhamList = sanPhamList ?? new List<SanPhamDto>();
        _bienTheList = _sanPhamList.SelectMany(x => x.BienThe).ToList();
        _toppingList = toppingList ?? new List<ToppingDto>();

        EnsureDriver();
    }

    private void EnsureDriver()
    {
        if (_driver != null) return;

        var options = new ChromeOptions();
        options.AddArgument("--headless=new");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");

        // BẮT PERFORMANCE LOGS để lấy sự kiện Network.*
        options.SetLoggingPreference(LogType.Performance, LogLevel.All);

        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;
        service.SuppressInitialDiagnosticInformation = true;

        _driver = new ChromeDriver(service, options);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));

        // Bật Network qua CDP
        try { _driver.ExecuteCdpCommand("Network.enable", new Dictionary<string, object>()); } catch { }

        _sniffer = new PerfCdpSniffer(_driver);
    }

    public HoaDonDto GetFirstOrderPopup()
    {
        // Cho phép thử tối đa 2 lần:
        //  - Lần 1: bình thường
        //  - Nếu fail / status COMPLETE -> reset + thử lại 1 lần nữa
        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (_driver == null || _wait == null) EnsureDriver();

            var driver = _driver!;
            var wait = _wait!;

            // ==== LOGIN CHỈ 1 LẦN CHO MỖI SESSION ====
            if (!_isAuthenticated)
            {
                driver.Navigate().GoToUrl("https://store.shippershipping.com");

                // load cookie 1 lần
                if (File.Exists(_cookieFile))
                {
                    try
                    {
                        var json = File.ReadAllText(_cookieFile);
                        var cookies = JsonSerializer.Deserialize<List<CookieData>>(json);
                        foreach (var c in cookies ?? new List<CookieData>())
                        {
                            if (!string.IsNullOrEmpty(c.Name) && !string.IsNullOrEmpty(c.Value))
                            {
                                driver.Manage().Cookies.AddCookie(
                                    new Cookie(c.Name, c.Value, c.Domain, c.Path, c.Expiry));
                            }
                        }

                        driver.Navigate().Refresh();
                    }
                    catch
                    {
                        // ignore cookie errors
                    }
                }

                try
                {
                    // nếu thấy avatar thì coi như đã login
                    wait.Until(d => d.FindElements(By.XPath(avatarXPath)).Count > 0);
                    _isAuthenticated = true;
                }
                catch
                {
                    // chưa login hoặc cookie hết hạn -> login tay
                    if (!IsLoggedIn(driver))
                    {
                        Login(driver, wait);
                        _isAuthenticated = true;
                    }
                }
            }

            // ==== TỪ ĐÂY: CHỈ VÀO TRANG ORDER & BẮT PAYLOAD ====
            driver.Navigate().GoToUrl(orderPageUrl);

            // đảm bảo không còn popup cũ che nút "Xem chi tiết"
            CloseOldPopupIfAny(driver);

            // nếu không thấy nút "xem chi tiết" -> có thể session hỏng, thử reset nếu còn lượt
            try
            {
                wait.Until(d => d.FindElements(By.XPath(xemChiTietRow1XPath)).Count > 0);
            }
            catch
            {
                if (attempt == 0)
                {
                    // reset session + thử lại
                    ForceResetSession();
                    continue;
                }

                // hết lượt thử -> trả về hoá đơn fallback
                var nowFail = DateTime.Now;
                return new HoaDonDto
                {
                    Id = Guid.Empty,
                    Ngay = nowFail.Date,
                    KhachHangId = Guid.Parse("D6A1CFA4-E070-4599-92C2-884CD6469BF4"),
                    NgayGio = nowFail,
                    MaHoaDon = MaHoaDonGenerator.Generate(),
                    PhanLoai = "App",
                    DiaChiText = "",
                    ChiTietHoaDons = new ObservableCollection<ChiTietHoaDonDto>(),
                    GhiChu = "⚠️ Không đọc được danh sách đơn từ app shipping (không thấy nút Xem chi tiết)."
                };
            }

            // ==== BẮT PAYLOAD SAU KHI CLICK "Xem chi tiết" ====
            _sniffer!.Flush();

            bool UrlFilter(string url) =>
                url.Contains("/v1/store/orderFood", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("/v1/store/orderDetail", StringComparison.OrdinalIgnoreCase) ||
                url.Contains("/graphql", StringComparison.OrdinalIgnoreCase);

            bool BodyFilter(string body)
            {
                if (string.IsNullOrWhiteSpace(body)) return false;

                // loại các payload thống kê / dashboard
                if (body.Contains("\"totalIncome\"", StringComparison.OrdinalIgnoreCase) &&
                    body.Contains("\"totalRevenue\"", StringComparison.OrdinalIgnoreCase))
                    return false;

                // chi tiết đơn thường có "details" + "store" hoặc "customer"
                if (!body.Contains("\"details\"", StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!body.Contains("\"store\"", StringComparison.OrdinalIgnoreCase) &&
                    !body.Contains("\"customer\"", StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            }

            var ctsPayload = new CancellationTokenSource(TimeSpan.FromSeconds(12));
            var waitJsonTask = _sniffer.WaitForJsonAsync(
                UrlFilter,
                BodyFilter,
                onlyXhr: true,
                timeoutMs: 12000,
                ct: ctsPayload.Token
            );

            // dùng wait để chắc chắn element click được, tránh bị overlay che
            var xemChiTiet = wait.Until(d =>
            {
                try
                {
                    var el = d.FindElement(By.XPath(xemChiTietRow1XPath));
                    return (el.Displayed && el.Enabled) ? el : null;
                }
                catch
                {
                    return null;
                }
            });
            xemChiTiet.Click();

            // Chờ popup xuất hiện (đảm bảo request đã chạy)
            wait.Until(d =>
            {
                var elems = d.FindElements(By.XPath(chiTietPopupXPath));
                return elems.Count > 0 && elems[0].Displayed;
            });

            (string Url, string Json)? jsonHit = null;
            try
            {
                jsonHit = waitJsonTask.GetAwaiter().GetResult();
            }
            catch
            {
                // ignore
            }

            // ========== TRƯỜNG HỢP KHÔNG BẮT ĐƯỢC PAYLOAD ==========
            if (jsonHit is null)
            {
                // đóng popup nếu có
                CloseOldPopupIfAny(driver);

                if (attempt == 0)
                {
                    // lần đầu fail -> reset + thử lại
                    ForceResetSession();
                    continue;
                }

                // lần 2 vẫn fail -> trả fallback
                var nowFallback = DateTime.Now;
                return new HoaDonDto
                {
                    Id = Guid.Empty,
                    Ngay = nowFallback.Date,
                    KhachHangId = Guid.Parse("D6A1CFA4-E070-4599-92C2-884CD6469BF4"),
                    NgayGio = nowFallback,
                    MaHoaDon = MaHoaDonGenerator.Generate(),
                    PhanLoai = "App",
                    DiaChiText = "",
                    ChiTietHoaDons = new ObservableCollection<ChiTietHoaDonDto>(),
                    GhiChu = "⚠️ Không đọc được payload từ app shipping sau khi reset session."
                };
            }

            // gửi JSON lên Discord để theo dõi (không pretty để nhanh)
            ShowJsonPopup("Shipping JSON", jsonHit.Value.Json);

            // ====== PARSE PAYLOAD ======
            var chiTietPayloads = ExtractDetailsFromPayload(jsonHit.Value.Json);
            var chiTiets = new List<ChiTietHoaDonDto>();

            // Thông tin chung
            string code = "";
            string tenKH = "";
            string diaChi = "";
            string taiXe = "";
            string trangThai = "";
            string tongTien = "";

            string? customerPhone = null;
            string? customerNote = null;
            string? internalId = null;
            decimal? serviceFee = null;
            decimal? shipFee = null;
            decimal? voucherValue = null;

            try
            {
                using var doc = JsonDocument.Parse(jsonHit.Value.Json);
                var root = doc.RootElement;

                JsonElement orderNode = root;
                if (root.TryGetProperty("data", out var dataNode) && dataNode.ValueKind == JsonValueKind.Object)
                    orderNode = dataNode;

                if (orderNode.ValueKind != JsonValueKind.Undefined)
                {
                    // Mã đơn
                    code = TryGetString(orderNode, "code") ?? "";

                    // Địa chỉ giao
                    diaChi = TryGetString(orderNode, "startAddress", "address") ?? "";

                    // Tổng tiền: moneyTotal
                    var totalMoney = TryGetDecimal(orderNode, "moneyTotal");
                    if (totalMoney is not null && totalMoney.Value > 0)
                        tongTien = $"{totalMoney.Value:#,0}";

                    // Trạng thái
                    trangThai = TryGetString(orderNode, "status") ?? "";

                    // *** NẾU ĐƠN ĐÃ COMPLETE THÌ RESET VÀ THỬ LẠI ***
                    if (attempt == 0 &&
                        !string.IsNullOrEmpty(trangThai) &&
                        trangThai.Equals("COMPLETE", StringComparison.OrdinalIgnoreCase))
                    {
                        // đóng popup cũ rồi reset
                        CloseOldPopupIfAny(driver);
                        ForceResetSession();
                        continue; // quay lại vòng for, thử lần 2
                    }

                    // Thông tin khách
                    customerPhone = TryGetString(orderNode, "customerPhone", "phone", "customer_mobile");

                    if (orderNode.TryGetProperty("customer", out var custNode) &&
                        custNode.ValueKind == JsonValueKind.Object)
                    {
                        customerPhone ??= TryGetString(custNode, "phone", "mobile", "customerPhone");
                        tenKH = TryGetString(custNode, "name")?.Trim() ?? tenKH;
                    }

                    // Ghi chú khách
                    customerNote = TryGetString(orderNode, "note", "customerNote", "instruction", "customer_note");

                    internalId = TryGetString(orderNode, "id", "orderId");

                    // Tài xế
                    if (orderNode.TryGetProperty("driver", out var drvNode) &&
                        drvNode.ValueKind == JsonValueKind.Object)
                    {
                        taiXe = TryGetString(drvNode, "name")?.Trim() ?? taiXe;
                    }

                    // Phí dịch vụ & ship
                    serviceFee = TryGetDecimal(orderNode, "serviceFee", "platformFee", "service_fee");
                    shipFee = TryGetDecimal(orderNode, "shipFee", "shippingFee", "ship_fee");
                    shipFee ??= TryGetDecimal(orderNode, "moneyDistance");

                    // Voucher
                    if (orderNode.TryGetProperty("voucher", out var vch) &&
                        vch.ValueKind == JsonValueKind.Object)
                    {
                        voucherValue = TryGetDecimal(vch, "value", "amount");
                    }
                    else
                    {
                        voucherValue ??= TryGetDecimal(orderNode, "voucher", "discount");
                    }
                }
            }
            catch
            {
                // ignore parse errors, sẽ có default
            }

            // ====== TẠO CHI TIẾT HÓA ĐƠN TỪ PAYLOAD ======
            foreach (var pd in chiTietPayloads)
            {
                string tenSP = pd.Name?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(tenSP)) continue;

                int soLuong = pd.Amount > 0 ? pd.Amount : 1;
                decimal donGiaWeb = pd.FinalPrice > 0 ? pd.FinalPrice : 0;

                string? tenBienThe = null;
                if (!string.IsNullOrWhiteSpace(pd.VariationNameRaw))
                {
                    tenBienThe = StringHelper.MyNormalizeText(pd.VariationNameRaw).ToLower().Trim();
                }

                Guid bienTheId = MapSanPhamBienTheId(tenSP, tenBienThe, donGiaWeb);
                if (bienTheId == Guid.Empty)
                    continue;

                var bienThe = _bienTheList.FirstOrDefault(b => b.Id == bienTheId);

                var toppingDtos = new List<ToppingDto>();
                foreach (var tNameRaw in pd.ToppingNames)
                {
                    if (string.IsNullOrWhiteSpace(tNameRaw)) continue;
                    string norm = StringHelper.MyNormalizeText(tNameRaw).ToLower();

                    toppingDtos.Add(new ToppingDto
                    {
                        Id = MapToppingId(norm),
                        Ten = norm,
                        Gia = GetToppingGia(norm),
                        SoLuong = 1
                    });
                }

                var ct = new ChiTietHoaDonDto
                {
                    Id = Guid.NewGuid(),
                    SanPhamIdBienThe = bienTheId,
                    TenSanPham = tenSP,
                    TenBienThe = tenBienThe ?? (bienThe?.TenBienThe ?? ""),
                    DonGia = bienThe?.GiaBan ?? donGiaWeb,
                    SoLuong = soLuong,
                    ToppingDtos = toppingDtos
                };

                chiTiets.Add(ct);
            }

            var now = DateTime.Now;

            // đóng popup chi tiết để lần sau không bị che nút
            CloseOldPopupIfAny(driver);

            // Nếu tới đây nghĩa là:
            //  - Đã có payload hợp lệ
            //  - Và (status không COMPLETE) hoặc đang ở lần thử thứ 2
            return new HoaDonDto
            {
                Id = Guid.Empty,
                Ngay = now.Date,
                KhachHangId = Guid.Parse("D6A1CFA4-E070-4599-92C2-884CD6469BF4"),
                DiaChiText = taiXe,
                NgayGio = now,
                MaHoaDon = string.IsNullOrWhiteSpace(code) ? MaHoaDonGenerator.Generate() : code,
                PhanLoai = "App",
                ChiTietHoaDons = new ObservableCollection<ChiTietHoaDonDto>(chiTiets),
                GhiChu =
                    $"• {tongTien}\n" +
                (string.IsNullOrEmpty(customerNote) ? "" : $"\n🟟• {customerNote}") +
                    $"• {diaChi}"
                //+ (string.IsNullOrEmpty(customerPhone) ? "" : $"\n• {tenKH} - {customerPhone}")
                //+ (serviceFee is null ? "" : $"\n⚙️ Phí DV: {serviceFee:#,0}")
                //+ (shipFee is null ? "" : $"\n🟟 Ship: {shipFee:#,0}")
                //+ (voucherValue is null ? "" : $"\n🟟️ Voucher: -{voucherValue:#,0}")
                //+ (string.IsNullOrEmpty(trangThai) ? "" : $"\nTrạng thái: {trangThai}")
                //+ (string.IsNullOrEmpty(internalId) ? "" : $"\n#ID nội bộ: {internalId}")
            };
        }

        // Nếu vì lý do gì đó vẫn thoát khỏi for mà chưa return, trả fallback.
        var nowFallbackFinal = DateTime.Now;
        return new HoaDonDto
        {
            Id = Guid.Empty,
            Ngay = nowFallbackFinal.Date,
            KhachHangId = Guid.Parse("D6A1CFA4-E070-4599-92C2-884CD6469BF4"),
            NgayGio = nowFallbackFinal,
            MaHoaDon = MaHoaDonGenerator.Generate(),
            PhanLoai = "App",
            DiaChiText = "",
            ChiTietHoaDons = new ObservableCollection<ChiTietHoaDonDto>(),
            GhiChu = "⚠️ AppShipping: không thể bắt đơn mới sau 2 lần thử."
        };
    }

    private void ForceResetSession()
    {
        // reset toàn bộ driver + auth, lần gọi sau sẽ EnsureDriver + login lại
        AppShippingHelperText.DisposeDriver();
        _isAuthenticated = false;
    }
    private void CloseOldPopupIfAny(IWebDriver driver)
    {
        try
        {
            var wrappers = driver.FindElements(By.CssSelector("div.el-dialog__wrapper"));
            foreach (var wrapper in wrappers)
            {
                if (!wrapper.Displayed) continue;

                var closeButtons = wrapper.FindElements(By.CssSelector(".el-dialog__headerbtn"));
                foreach (var btn in closeButtons)
                {
                    if (btn.Displayed && btn.Enabled)
                    {
                        btn.Click();
                        System.Threading.Thread.Sleep(150);
                    }
                }
            }

            try
            {
                driver.FindElement(By.TagName("body")).SendKeys(Keys.Escape);
            }
            catch { }
        }
        catch
        {
            // ignore
        }
    }
    public static void DisposeDriver()
    {
        try
        {
            if (_driver != null)
            {
                _driver.Quit();
                _driver.Dispose();
            }
        }
        catch { }
        finally
        {
            _driver = null;
            _wait = null;
            _isAuthenticated = false;

            foreach (var process in Process.GetProcessesByName("chromedriver"))
            {
                try { process.Kill(); } catch { }
            }
        }
    }

    private bool IsLoggedIn(IWebDriver driver) =>
        driver.FindElements(By.XPath(avatarXPath)).Count > 0;

    private void Login(IWebDriver driver, WebDriverWait wait)
    {
        // đảm bảo đang ở trang login
        driver.Navigate().GoToUrl("https://store.shippershipping.com");
        driver.FindElement(By.XPath(usernameXPath)).SendKeys(_username);
        driver.FindElement(By.XPath(passwordXPath)).SendKeys(_password);
        driver.FindElement(By.XPath(loginButtonXPath)).Click();
        wait.Until(d => d.FindElements(By.XPath(avatarXPath)).Count > 0);

        var cookies = driver.Manage().Cookies.AllCookies;
        File.WriteAllText(_cookieFile, JsonSerializer.Serialize(cookies));
    }

    private Guid MapSanPhamBienTheId(string tenSanPham, string? tenBienThe, decimal donGiaWeb)
    {
        var sp = _sanPhamList.FirstOrDefault(x =>
            StringHelper.MyNormalizeText(x.Ten).ToLower() ==
            StringHelper.MyNormalizeText(tenSanPham.Replace("Trân Châu Đường Đen", "TCĐĐ")).ToLower());

        if (sp == null)
        {
            MessageBox.Show(
                $"Vui lòng tự nhập thêm món: {tenSanPham}",
                "Cảnh báo mapping sản phẩm",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            DiscordService.SendAsync(TraSuaApp.Shared.Enums.DiscordEventType.Admin,
                $"{tenSanPham} AppShippingError");
            return Guid.Empty;
        }

        if (!string.IsNullOrEmpty(tenBienThe))
            tenBienThe = StringHelper.MyNormalizeText(tenBienThe).ToLower()
                .Replace("x 1", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

        var bienThe = _bienTheList.Where(b => b.SanPhamId == sp.Id)
            .FirstOrDefault(b =>
                StringHelper.MyNormalizeText(b.TenBienThe ?? "").ToLower() ==
                (tenBienThe ?? "").ToLower());

        if (bienThe == null && donGiaWeb > 0)
            bienThe = sp.BienThe.FirstOrDefault(b => b.GiaBan == donGiaWeb);

        if (bienThe == null)
            bienThe = sp.BienThe.FirstOrDefault(b => b.MacDinh);

        if (bienThe == null)
        {
            MessageBox.Show(
                $"⚠️ Sản phẩm {tenSanPham} không có biến thể phù hợp. Giá web: {donGiaWeb}",
                "Cảnh báo mapping biến thể",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return Guid.Empty;
        }

        return bienThe.Id;
    }

    private Guid MapToppingId(string tenTopping)
    {
        var tp = _toppingList.FirstOrDefault(t =>
            StringHelper.MyNormalizeText(t.Ten).ToLower() ==
            StringHelper.MyNormalizeText(tenTopping).ToLower());

        return tp?.Id ?? Guid.Empty;
    }

    private decimal GetToppingGia(string tenTopping)
    {
        return _toppingList
            .FirstOrDefault(t =>
                StringHelper.MyNormalizeText(t.Ten).ToLower() ==
                StringHelper.MyNormalizeText(tenTopping).ToLower())
            ?.Gia ?? 0;
    }

    // ===== JSON helpers =====
    private static string? TryGetString(JsonElement node, params string[] keys)
    {
        foreach (var k in keys)
            if (node.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        return null;
    }

    private static decimal? TryGetDecimal(JsonElement node, params string[] keys)
    {
        foreach (var k in keys)
            if (node.TryGetProperty(k, out var v) && v.ValueKind is JsonValueKind.Number)
                if (v.TryGetDecimal(out var d)) return d;
        return null;
    }

    // ===== Logging helpers =====
    private static void ShowJsonPopup(string title, string rawJson)
    {
        try
        {
            var pretty = TryPrettyJson(rawJson);
            var redacted = RedactSecrets(pretty);
            var text = TruncateForMessageBox(redacted, 100000);

            // Gửi Discord để theo dõi
            DiscordService.SendAsync(TraSuaApp.Shared.Enums.DiscordEventType.Admin, text);

#if DEBUG
            // Chỉ popup khi DEBUG để tránh làm chậm bản release
            if (Application.Current?.Dispatcher != null &&
                !Application.Current.Dispatcher.CheckAccess())
            {
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information));
            }
            else
            {
                MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
#endif
        }
        catch
        {
#if DEBUG
            MessageBox.Show(rawJson, title, MessageBoxButton.OK, MessageBoxImage.Information);
#endif
        }
    }

    private static string TryPrettyJson(string s)
    {
        try
        {
            using var doc = JsonDocument.Parse(s);
            var opts = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(doc.RootElement, opts);
        }
        catch
        {
            return Regex.Replace(s, @"\\u(?<code>[0-9a-fA-F]{4})", m =>
            {
                var code = Convert.ToInt32(m.Groups["code"].Value, 16);
                return char.ConvertFromUtf32(code);
            });
        }
    }

    private static string RedactSecrets(string s)
    {
        try
        {
            s = Regex.Replace(
                s, "(?i)(\"(?:password|access_?token|refresh_?token|authorization)\"\\s*:\\s*\")([^\"]+)(\")",
                "$1***$3");

            s = Regex.Replace(
                s, "(?i)(Bearer\\s+)[A-Za-z0-9\\-_.=]+", "$1***");

            return s;
        }
        catch { return s; }
    }

    private static string TruncateForMessageBox(string s, int maxChars)
    {
        if (string.IsNullOrEmpty(s) || s.Length <= maxChars) return s;
        return s.Substring(0, maxChars) + "\n...\n[truncated]";
    }

    // ============================
    //  MODEL + PARSER PAYLOAD
    // ============================

    private sealed class ShippingDetailPayload
    {
        public string Name { get; set; } = "";
        public int Amount { get; set; } = 1;
        public decimal FinalPrice { get; set; }
        public string? VariationNameRaw { get; set; }
        public decimal? VariationExtra { get; set; }
        public List<string> ToppingNames { get; } = new();
    }

    private static bool LooksLikeSize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var norm = StringHelper.MyNormalizeText(name).ToLower();

        if (norm.Contains("size")) return true;
        if (Regex.IsMatch(norm, @"\b(s|m|l|xl|xs|xxl)\b")) return true;

        return false;
    }

    /// <summary>
    /// Đọc mảng details từ payload JSON để lấy: tên món, số lượng, đơn giá, biến thể (Size L,...), topping.
    /// </summary>
    private static List<ShippingDetailPayload> ExtractDetailsFromPayload(string rawJson)
    {
        var result = new List<ShippingDetailPayload>();

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            JsonElement data = root;
            if (root.TryGetProperty("data", out var d) && d.ValueKind == JsonValueKind.Object)
                data = d;

            JsonElement details;
            if (!data.TryGetProperty("details", out details) || details.ValueKind != JsonValueKind.Array)
            {
                if (!root.TryGetProperty("details", out details) || details.ValueKind != JsonValueKind.Array)
                    return result;
            }

            foreach (var item in details.EnumerateArray())
            {
                string? name = null;

                if (item.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                {
                    name = nameEl.GetString();
                }
                else if (item.TryGetProperty("food", out var foodEl) && foodEl.ValueKind == JsonValueKind.Object)
                {
                    if (foodEl.TryGetProperty("name", out var fn) && fn.ValueKind == JsonValueKind.String)
                        name = fn.GetString();
                }

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                int amount = 1;
                if (item.TryGetProperty("amount", out var amtEl) && amtEl.ValueKind == JsonValueKind.Number)
                {
                    if (!amtEl.TryGetInt32(out amount)) amount = 1;
                    if (amount <= 0) amount = 1;
                }

                decimal finalPrice = 0;
                if (item.TryGetProperty("finalPrice", out var priceEl) && priceEl.ValueKind == JsonValueKind.Number)
                {
                    priceEl.TryGetDecimal(out finalPrice);
                }

                var variantNames = new List<string>();
                var toppingNames = new List<string>();
                decimal variationExtra = 0;

                // orderFoodVariationDetails: size + có thể cả topping
                if (item.TryGetProperty("orderFoodVariationDetails", out var varArr) &&
                    varArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var od in varArr.EnumerateArray())
                    {
                        string? vName = null;

                        if (od.TryGetProperty("foodVariation", out var fv) && fv.ValueKind == JsonValueKind.Object)
                        {
                            if (fv.TryGetProperty("name", out var vn) && vn.ValueKind == JsonValueKind.String)
                                vName = vn.GetString();

                            if (fv.TryGetProperty("price", out var p1) && p1.ValueKind == JsonValueKind.Number &&
                                p1.TryGetDecimal(out var extra1))
                            {
                                variationExtra += extra1;
                            }
                        }
                        else if (od.TryGetProperty("price", out var p2) && p2.ValueKind == JsonValueKind.Number &&
                                 p2.TryGetDecimal(out var extra2))
                        {
                            variationExtra += extra2;
                        }

                        if (!string.IsNullOrWhiteSpace(vName))
                        {
                            if (LooksLikeSize(vName))
                                variantNames.Add(vName);
                            else
                                toppingNames.Add(vName);
                        }
                    }
                }

                // foodVariations: thường là topping
                if (item.TryGetProperty("foodVariations", out var fvArr) &&
                    fvArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var fvItem in fvArr.EnumerateArray())
                    {
                        string? tName = null;

                        if (fvItem.ValueKind == JsonValueKind.String)
                        {
                            tName = fvItem.GetString();
                        }
                        else if (fvItem.ValueKind == JsonValueKind.Object)
                        {
                            if (fvItem.TryGetProperty("name", out var tn) && tn.ValueKind == JsonValueKind.String)
                                tName = tn.GetString();
                        }

                        if (!string.IsNullOrWhiteSpace(tName))
                        {
                            if (LooksLikeSize(tName))
                                variantNames.Add(tName);
                            else
                                toppingNames.Add(tName);
                        }
                    }
                }

                string? variationName = variantNames.Count > 0
                    ? string.Join(" + ", variantNames)
                    : null;

                var detail = new ShippingDetailPayload
                {
                    Name = name ?? "",
                    Amount = amount,
                    FinalPrice = finalPrice,
                    VariationNameRaw = variationName,
                    VariationExtra = variationExtra > 0 ? variationExtra : null
                };

                foreach (var t in toppingNames.Distinct())
                    detail.ToppingNames.Add(t);

                result.Add(detail);
            }
        }
        catch
        {
            // lỗi parse -> trả list rỗng
        }

        return result;
    }
}

// ==============================
//   PerfCdpSniffer
// ==============================
public sealed class PerfCdpSniffer
{
    private readonly ChromeDriver _driver;

    public PerfCdpSniffer(ChromeDriver driver) => _driver = driver;

    // Dọn backlog log cũ trước khi chờ payload mới
    public void Flush()
    {
        try { _driver.Manage().Logs.GetLog(LogType.Performance); } catch { }
    }

    public async Task<(string Url, string Json)?> WaitForJsonAsync(
        Func<string, bool> urlFilter,
        Func<string, bool>? bodyFilter = null,
        bool onlyXhr = true,
        int timeoutMs = 10000,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var seenRequest = new HashSet<string>(StringComparer.Ordinal);

        while (sw.ElapsedMilliseconds < timeoutMs && !ct.IsCancellationRequested)
        {
            var entries = _driver.Manage().Logs.GetLog(LogType.Performance);
            foreach (var entry in entries)
            {
                try
                {
                    using var doc = JsonDocument.Parse(entry.Message);
                    if (!doc.RootElement.TryGetProperty("message", out var msg)) continue;

                    var method = msg.GetProperty("method").GetString();
                    if (!string.Equals(method, "Network.responseReceived", StringComparison.Ordinal))
                        continue;

                    var prms = msg.GetProperty("params");

                    // Chỉ XHR/Fetch nếu cần (giảm nhiễu)
                    if (onlyXhr && prms.TryGetProperty("type", out var tEl))
                    {
                        var type = tEl.GetString() ?? "";
                        if (!type.Equals("XHR", StringComparison.OrdinalIgnoreCase) &&
                            !type.Equals("Fetch", StringComparison.OrdinalIgnoreCase))
                            continue;
                    }

                    var response = prms.GetProperty("response");
                    var url = response.GetProperty("url").GetString() ?? "";
                    var mime = response.TryGetProperty("mimeType", out var mm) ? mm.GetString() ?? "" : "";
                    var requestId = prms.GetProperty("requestId").GetString() ?? "";

                    if (seenRequest.Contains(requestId)) continue;
                    if (!mime.Contains("json", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!urlFilter(url)) continue;

                    seenRequest.Add(requestId);

                    var resObj = _driver.ExecuteCdpCommand("Network.getResponseBody",
                        new Dictionary<string, object> { { "requestId", requestId } });

                    string body = "";
                    bool base64 = false;

                    if (resObj is Dictionary<string, object> dict)
                    {
                        if (dict.TryGetValue("body", out var bodyObj) && bodyObj is string s)
                            body = s;

                        if (dict.TryGetValue("base64Encoded", out var b64Obj))
                        {
                            if (b64Obj is bool b) base64 = b;
                            else if (b64Obj is string sb && bool.TryParse(sb, out var b2)) base64 = b2;
                        }
                    }
                    else if (resObj is System.Collections.IDictionary idict)
                    {
                        if (idict.Contains("body")) body = idict["body"]?.ToString() ?? "";
                        if (idict.Contains("base64Encoded"))
                        {
                            var v = idict["base64Encoded"];
                            if (v is bool b) base64 = b;
                            else if (v is string sb && bool.TryParse(sb, out var b2)) base64 = b2;
                        }
                    }

                    if (base64 && !string.IsNullOrEmpty(body))
                    {
                        try { body = Encoding.UTF8.GetString(Convert.FromBase64String(body)); }
                        catch { /* ignore */ }
                    }

                    if (string.IsNullOrWhiteSpace(body)) continue;

                    if (bodyFilter == null || bodyFilter(body))
                        return (url, body);
                }
                catch
                {
                    // bỏ qua entry lỗi
                }
            }

            await Task.Delay(80, ct);
        }

        return null;
    }
}

// ==============================
//   Cookie model
// ==============================
public class CookieData
{
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("value")] public string Value { get; set; }
    [JsonPropertyName("domain")] public string Domain { get; set; }
    [JsonPropertyName("path")] public string Path { get; set; }
    [JsonPropertyName("expiry")] public DateTime? Expiry { get; set; }
    [JsonPropertyName("secure")] public bool Secure { get; set; }
    [JsonPropertyName("httpOnly")] public bool HttpOnly { get; set; }
    [JsonPropertyName("sameSite")] public string SameSite { get; set; }
}

// ==============================
//   FACTORY / HOST ĐƠN GIẢN
// ==============================
internal static class AppShippingHelperFactory
{
    private static Task<AppShippingHelperText>? _instanceTask;
    private static readonly object _lock = new();

    public static Task<AppShippingHelperText> CreateAsync(string username, string password)
    {
        lock (_lock)
        {
            _instanceTask ??= InitializeAsync(username, password);
            return _instanceTask;
        }
    }

    public static Task<AppShippingHelperText> GetAsync()
    {
        if (_instanceTask == null) throw new Exception("App Shopping chưa sẵn sàng.");
        return _instanceTask;
    }

    public static void Reset()
    {
        _instanceTask = null;
        AppShippingHelperText.DisposeDriver();
    }

    private static async Task<AppShippingHelperText> InitializeAsync(string username, string password)
    {
        await AppProviders.EnsureCreatedAsync();

        if (AppProviders.SanPhams == null || AppProviders.Toppings == null)
            throw new Exception("Providers chưa sẵn sàng.");

        await Task.WhenAll(
            AppProviders.SanPhams.InitializeAsync(),
            AppProviders.Toppings.InitializeAsync()
        );

        var sanPhams = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();
        var toppings = AppProviders.Toppings.Items.ToList();

        return new AppShippingHelperText(username, password, sanPhams, toppings);
    }
}