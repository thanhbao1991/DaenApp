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

    // --- XPath & URL ---
    private readonly string usernameXPath = "//*[@id='app']/div/form/div[2]/div/div[1]/input";
    private readonly string passwordXPath = "//*[@id='app']/div/form/div[3]/div/div[1]/input";
    private readonly string loginButtonXPath = "/html/body/div/div/form/button";
    private readonly string avatarXPath = "//*[@id=\"app\"]/div/div[2]/div/div/div[3]/div/div/img";
    private readonly string orderPageUrl = "https://store.shippershipping.com/#/store/order";
    private readonly string CodeRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[2]/div/p";
    private readonly string TrangThaiRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[2]/div/span";
    private readonly string TenTaiXeRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[3]/div/ul/li/span";
    private readonly string TongTienRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[5]/div/div[2]/span";
    private readonly string TenKhachHangRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[4]/div/ul/li[1]/span";
    private readonly string DiaChiRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[4]/div/ul/li[2]/span";
    private readonly string XemChiTietRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[5]/div/div[1]/a/span";
    private readonly string ChiTietPopupXPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[6]/div";

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

        // Bật Network qua CDP (không cần DevTools typed)
        try { _driver.ExecuteCdpCommand("Network.enable", new Dictionary<string, object>()); } catch { }

        // Sniffer dựa trên Performance Logs
        _sniffer = new PerfCdpSniffer(_driver);
    }

    public HoaDonDto GetFirstOrderPopup()
    {
        if (_driver == null || _wait == null) EnsureDriver();

        var driver = _driver!;
        var wait = _wait!;

        driver.Navigate().GoToUrl("https://store.shippershipping.com");

        // load cookie
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
                        driver.Manage().Cookies.AddCookie(new Cookie(c.Name, c.Value, c.Domain, c.Path, c.Expiry));
                    }
                }
                driver.Navigate().Refresh();
                wait.Until(d => d.FindElements(By.XPath(avatarXPath)).Count > 0);
            }
            catch
            {
                // ignore cookie errors
            }
        }

        if (!IsLoggedIn(driver))
        {
            Login(driver, wait);
        }

        driver.Navigate().GoToUrl(orderPageUrl);

        // Chờ bảng load
        wait.Until(d => d.FindElements(By.XPath(CodeRow1XPath)).Count > 0);

        string code = driver.FindElement(By.XPath(CodeRow1XPath)).Text.Trim();
        string tenKH = driver.FindElement(By.XPath(TenKhachHangRow1XPath)).Text.Trim();
        string diaChi = driver.FindElement(By.XPath(DiaChiRow1XPath)).Text.Trim();
        string taiXe = driver.FindElement(By.XPath(TenTaiXeRow1XPath)).Text.Trim();
        string trangThai = driver.FindElement(By.XPath(TrangThaiRow1XPath)).Text.Trim();
        string tongTien = driver.FindElement(By.XPath(TongTienRow1XPath)).Text.Trim();

        // ==== BẮT PAYLOAD ẨN SAU KHI CLICK "Xem chi tiết" ====
        // 1) Xoá backlog để chỉ lấy request mới phát sinh do click
        _sniffer!.Flush();

        // 2) Filter theo URL: siết vào nhóm order detail (sửa theo endpoint thực tế nếu biết chắc)
        bool UrlFilter(string url) =>
            url.Contains("/v1/store/orderFood", StringComparison.OrdinalIgnoreCase)
            || url.Contains("/v1/store/orderDetail", StringComparison.OrdinalIgnoreCase)
            || url.Contains("/graphql", StringComparison.OrdinalIgnoreCase);

        // 3) Filter body: loại payload thống kê, bắt buộc chứa code của đơn đang xem
        bool BodyFilter(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return false;

            // loại thống kê
            if (body.Contains("\"totalIncome\"", StringComparison.OrdinalIgnoreCase) &&
                body.Contains("\"totalRevenue\"", StringComparison.OrdinalIgnoreCase))
                return false;

            // phải chứa code vừa lấy từ dòng
            if (!string.IsNullOrEmpty(code) &&
                !body.Contains($"\"code\":\"{code}\"", StringComparison.OrdinalIgnoreCase))
                return false;

            // gợi ý các key thường thấy của payload chi tiết
            if (body.Contains("\"foodList\"", StringComparison.OrdinalIgnoreCase)) return true;
            if (body.Contains("\"items\"", StringComparison.OrdinalIgnoreCase)) return true;
            if (body.Contains("\"product\"", StringComparison.OrdinalIgnoreCase)) return true;
            if (body.Contains("\"data\":[", StringComparison.OrdinalIgnoreCase)) return true;

            // hoặc có thông tin KH/ghi chú
            if (body.Contains("\"phone\"", StringComparison.OrdinalIgnoreCase)) return true;
            if (body.Contains("\"note\"", StringComparison.OrdinalIgnoreCase)) return true;
            if (body.Contains("\"store\"", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        // 4) Bắt JSON của request kế tiếp sau click
        var ctsPayload = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        var waitJsonTask = _sniffer.WaitForJsonAsync(
            UrlFilter,
            BodyFilter,
            onlyXhr: true,
            timeoutMs: 12000,
            ct: ctsPayload.Token
        );

        // 5) Click xem chi tiết -> server bắn XHR -> ta hứng body ở trên
        driver.FindElement(By.XPath(XemChiTietRow1XPath)).Click();

        // 6) Chờ popup hiện (DOM)
        var popup = wait.Until(d =>
        {
            var elems = d.FindElements(By.XPath(ChiTietPopupXPath));
            return (elems.Count > 0 && elems[0].Displayed) ? elems[0] : null;
        });

        // 7) Chờ payload JSON (nếu có)
        (string Url, string Json)? jsonHit = null;
        try { jsonHit = waitJsonTask.GetAwaiter().GetResult(); } catch { /* ignore */ }

        // 🟟 HIỂN THỊ BẢN JSON ĐÃ LỌC THEO YÊU CẦU
        if (jsonHit is not null)
        {
            try
            {
                var filtered = BuildFilteredShippingJson(jsonHit.Value.Json);
                ShowJsonPopup("Filtered Shipping JSON", filtered);
            }
            catch
            {
                // nếu có lỗi lọc thì fallback hiển thị bản thô
                ShowJsonPopup($"Payload JSON: {jsonHit.Value.Url}", jsonHit.Value.Json);
            }
        }

        // Parse các trường ẩn từ payload (giữ nguyên logic cũ)
        string? customerPhone = null, customerNote = null, internalId = null;
        decimal? serviceFee = null, shipFee = null, voucherValue = null;

        if (jsonHit is not null)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonHit.Value.Json);
                var root = doc.RootElement;

                // Tìm node order phổ biến: data.order / data.orderDetail / order
                JsonElement orderNode = default;
                if (root.TryGetProperty("data", out var data))
                {
                    if (data.TryGetProperty("order", out var ord)) orderNode = ord;
                    else if (data.TryGetProperty("orderDetail", out var ord2)) orderNode = ord2;
                    else if (data.EnumerateObject().FirstOrDefault().Value.ValueKind != JsonValueKind.Undefined)
                        orderNode = data.EnumerateObject().First().Value;
                }
                else
                {
                    orderNode = root;
                }

                if (orderNode.ValueKind != JsonValueKind.Undefined)
                {
                    customerPhone = TryGetString(orderNode, "customerPhone", "phone", "customer_mobile");
                    customerNote = TryGetString(orderNode, "note", "customerNote", "instruction", "customer_note");
                    internalId = TryGetString(orderNode, "id", "orderId", "code");

                    serviceFee = TryGetDecimal(orderNode, "serviceFee", "platformFee", "service_fee");
                    shipFee = TryGetDecimal(orderNode, "shipFee", "shippingFee", "ship_fee");

                    if (orderNode.TryGetProperty("voucher", out var vch))
                        voucherValue = TryGetDecimal(vch, "value", "amount");
                    else
                        voucherValue ??= TryGetDecimal(orderNode, "voucher", "discount");
                }
            }
            catch { /* ignore format khác */ }
        }

        // Lấy chi tiết món từ DOM (giữ như cũ)
        var itemElements = popup.FindElements(By.CssSelector("ul.food-list > li.food-item"));
        var chiTiets = new List<ChiTietHoaDonDto>();

        foreach (var item in itemElements)
        {
            string tenSP = item.FindElement(By.CssSelector("div.product-name > span.name")).Text.Trim();

            string qtyText = item.FindElement(By.CssSelector("div.product-order > span.quantity-badge"))
                                 .Text.Replace("x", "").Trim();
            int soLuong = int.TryParse(qtyText, out int qty) ? qty : 1;

            string priceText = item.FindElement(By.CssSelector("div.product-name span.single-price"))
                                   .Text.Replace(",", "").Trim();
            decimal donGia = decimal.TryParse(priceText, out decimal price) ? price : 0;

            var optionsDiv = item.FindElements(By.CssSelector("div.product-name > div > div"));
            string? tenBienThe = optionsDiv.FirstOrDefault()?.Text;
            if (!string.IsNullOrEmpty(tenBienThe))
            {
                tenBienThe = StringHelper.MyNormalizeText(tenBienThe).ToLower()
                    .Replace("x 1", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
            }

            Guid bienTheId = MapSanPhamBienTheId(tenSP, tenBienThe, donGia);
            if (bienTheId != Guid.Empty)
            {
                var bienThe = _bienTheList.FirstOrDefault(b => b.Id == bienTheId);

                var ct = new ChiTietHoaDonDto
                {
                    Id = Guid.NewGuid(),
                    SanPhamIdBienThe = bienTheId,
                    TenSanPham = tenSP,
                    TenBienThe = tenBienThe ?? "",
                    DonGia = bienThe?.GiaBan ?? donGia,
                    SoLuong = soLuong,
                    ToppingDtos = optionsDiv.Skip(1).Select(opt =>
                    {
                        string name = StringHelper.MyNormalizeText(opt.Text).ToLower();
                        return new ToppingDto
                        {
                            Id = MapToppingId(name),
                            Ten = name,
                            Gia = GetToppingGia(name),
                            SoLuong = 1
                        };
                    }).ToList()
                };

                chiTiets.Add(ct);
            }
        }

        var now = DateTime.Now;
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
                $"{tongTien}\n{diaChi}"
              + (string.IsNullOrEmpty(customerPhone) ? "" : $"\n🟟 {customerPhone}")
              + (string.IsNullOrEmpty(customerNote) ? "" : $"\n🟟️ {customerNote}")
              + (serviceFee is null ? "" : $"\n⚙️ Phí DV: {serviceFee:#,0}")
              + (shipFee is null ? "" : $"\n🟟 Ship: {shipFee:#,0}")
              + (voucherValue is null ? "" : $"\n🟟️ Voucher: -{voucherValue:#,0}")
              + (string.IsNullOrEmpty(internalId) ? "" : $"\n#ID nội bộ: {internalId}")
        };
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

    // ===== JSON helper =====
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

    // ===== MessageBox helpers =====
    private static void ShowJsonPopup(string title, string rawJson)
    {
        try
        {
            var pretty = TryPrettyJson(rawJson);
            var redacted = RedactSecrets(pretty);
            var text = TruncateForMessageBox(redacted, 100000); // nâng lên 100k ký tự
            DiscordService.SendAsync(TraSuaApp.Shared.Enums.DiscordEventType.Admin, pretty);
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
        }
        catch
        {
            MessageBox.Show(rawJson, title, MessageBoxButton.OK, MessageBoxImage.Information);
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
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping // hiện đúng tiếng Việt
            };
            return JsonSerializer.Serialize(doc.RootElement, opts);
        }
        catch
        {
            // Fallback unescape nếu server trả chuỗi JSON escaped
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
    //  JSON FILTER THEO YÊU CẦU
    // ============================
    private static string BuildFilteredShippingJson(string raw)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        JsonElement data = root;
        if (root.TryGetProperty("data", out var d)) data = d;

        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        // Top-level fields
        WriteStringIfExists(writer, "phone", data, "phone");
        WriteStringIfExists(writer, "startAddress", data, "startAddress");
        WriteNumberIfExists(writer, "distance", data, "distance");
        WriteNumberIfExists(writer, "startLong", data, "startLong");
        WriteNumberIfExists(writer, "startLat", data, "startLat");
        WriteNumberIfExists(writer, "moneyDistance", data, "moneyDistance");
        WriteNullableStringIfExists(writer, "note", data, "note");
        WriteStringIfExists(writer, "matrix", data, "matrix");
        WriteStringIfExists(writer, "status", data, "status");
        WriteNumberIfExists(writer, "totalFood", data, "totalFood");

        // customer
        writer.WritePropertyName("customer");
        if (data.TryGetProperty("customer", out var cust) && cust.ValueKind == JsonValueKind.Object)
        {
            writer.WriteStartObject();
            WriteStringIfExists(writer, "phone", cust, "phone");
            WriteStringIfExists(writer, "email", cust, "email");
            WriteStringIfExists(writer, "dayOfBirth", cust, "dayOfBirth");
            WriteStringIfExists(writer, "gender", cust, "gender");
            WriteStringIfExists(writer, "name", cust, "name");
            writer.WriteEndObject();
        }
        else
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }

        // details
        writer.WritePropertyName("details");
        if (data.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array)
        {
            writer.WriteStartArray();
            foreach (var item in details.EnumerateArray())
            {
                writer.WriteStartObject();

                // name nằm trong item.food.name
                string? name = null;
                if (item.TryGetProperty("food", out var food) && food.ValueKind == JsonValueKind.Object)
                {
                    if (food.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                        name = nameEl.GetString();
                }
                if (name is not null) writer.WriteString("name", name);

                // amount, finalPrice
                WriteNumberIfExists(writer, "amount", item, "amount");
                WriteNumberIfExists(writer, "finalPrice", item, "finalPrice");

                // arrays: foodVariations, orderFoodVariationDetails
                WriteArrayOrEmpty(writer, "foodVariations", item, "foodVariations");
                WriteArrayOrEmpty(writer, "orderFoodVariationDetails", item, "orderFoodVariationDetails");

                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        else
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static void WriteStringIfExists(Utf8JsonWriter w, string outName, JsonElement obj, string inName)
    {
        if (obj.TryGetProperty(inName, out var el))
        {
            if (el.ValueKind == JsonValueKind.String)
                w.WriteString(outName, el.GetString());
        }
    }

    private static void WriteNullableStringIfExists(Utf8JsonWriter w, string outName, JsonElement obj, string inName)
    {
        if (obj.TryGetProperty(inName, out var el))
        {
            if (el.ValueKind == JsonValueKind.String)
                w.WriteString(outName, el.GetString());
            else if (el.ValueKind == JsonValueKind.Null)
                w.WriteNull(outName);
        }
    }

    private static void WriteNumberIfExists(Utf8JsonWriter w, string outName, JsonElement obj, string inName)
    {
        if (!obj.TryGetProperty(inName, out var el)) return;
        if (el.ValueKind != JsonValueKind.Number) return;

        if (el.TryGetInt32(out int i)) w.WriteNumber(outName, i);
        else if (el.TryGetInt64(out long l)) w.WriteNumber(outName, l);
        else if (el.TryGetDouble(out double d)) w.WriteNumber(outName, d);
        else
        {
            // fallback: viết dạng string nếu kiểu số khó xác định (hiếm)
            w.WriteString(outName, el.ToString());
        }
    }

    private static void WriteArrayOrEmpty(Utf8JsonWriter w, string outName, JsonElement obj, string inName)
    {
        w.WritePropertyName(outName);
        if (obj.TryGetProperty(inName, out var el) && el.ValueKind == JsonValueKind.Array)
        {
            el.WriteTo(w);
        }
        else
        {
            w.WriteStartArray();
            w.WriteEndArray();
        }
    }
}

// ==============================
//   PerfCdpSniffer (không phụ thuộc Vxxx)
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

    /// <summary>
    /// Đợi JSON từ XHR/Fetch sau thời điểm gọi, với filter URL + body.
    /// </summary>
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
                    var prms = msg.GetProperty("params");

                    if (!string.Equals(method, "Network.responseReceived", StringComparison.Ordinal))
                        continue;

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