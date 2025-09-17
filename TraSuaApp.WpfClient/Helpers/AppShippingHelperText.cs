using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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

    public AppShippingHelperText(string username, string password)
    {
        _username = username;
        _password = password;

        _sanPhamList = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();
        _bienTheList = _sanPhamList.SelectMany(x => x.BienThe).ToList();
        _toppingList = AppProviders.Toppings.Items.ToList();

        EnsureDriver();
    }

    private void EnsureDriver()
    {
        if (_driver != null) return;

        var options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");

        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;
        service.SuppressInitialDiagnosticInformation = true;

        _driver = new ChromeDriver(service, options);
        _wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
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
            var json = File.ReadAllText(_cookieFile);
            var cookies = JsonSerializer.Deserialize<List<CookieData>>(json);
            foreach (var c in cookies)
            {
                if (!string.IsNullOrEmpty(c.Name) && !string.IsNullOrEmpty(c.Value))
                {
                    driver.Manage().Cookies.AddCookie(new Cookie(c.Name, c.Value, c.Domain, c.Path, c.Expiry));
                }
            }
            driver.Navigate().Refresh();
            wait.Until(d => d.FindElements(By.XPath(avatarXPath)).Count > 0);
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

        // Click xem chi tiết
        driver.FindElement(By.XPath(XemChiTietRow1XPath)).Click();

        // Chờ popup hiện
        var popup = wait.Until(d =>
        {
            var elems = d.FindElements(By.XPath(ChiTietPopupXPath));
            return (elems.Count > 0 && elems[0].Displayed) ? elems[0] : null;
        });

        // Lấy chi tiết món
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

            // Option (biến thể + topping)
            var optionsDiv = item.FindElements(By.CssSelector("div.product-name > div > div"));
            string? tenBienThe = optionsDiv.FirstOrDefault()?.Text;
            if (!string.IsNullOrEmpty(tenBienThe))
            {
                tenBienThe = TextSearchHelper.NormalizeText(tenBienThe).ToLower()
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
                    DonGia = bienThe?.GiaBan ?? donGia, // ưu tiên giá DB
                    SoLuong = soLuong,
                    ToppingDtos = optionsDiv.Skip(1).Select(opt =>
                    {
                        string name = TextSearchHelper.NormalizeText(opt.Text).ToLower();
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
            ChiTietHoaDons = chiTiets,
            GhiChu = $"{tongTien}\n{diaChi}"
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

            // Kill process nếu còn sót
            foreach (var process in System.Diagnostics.Process.GetProcessesByName("chromedriver"))
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
            TextSearchHelper.NormalizeText(x.Ten).ToLower() ==
            TextSearchHelper.NormalizeText(tenSanPham.Replace("Trân Châu Đường Đen", "TCĐĐ")).ToLower());

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
            tenBienThe = TextSearchHelper.NormalizeText(tenBienThe).ToLower()
                .Replace("x 1", "", StringComparison.OrdinalIgnoreCase)
                .Trim();

        var bienThe = _bienTheList.Where(b => b.SanPhamId == sp.Id)
            .FirstOrDefault(b =>
                TextSearchHelper.NormalizeText(b.TenBienThe ?? "").ToLower() ==
                (tenBienThe ?? "").ToLower());

        if (bienThe == null && donGiaWeb > 0)
        {
            bienThe = sp.BienThe.FirstOrDefault(b => b.GiaBan == donGiaWeb);
        }

        if (bienThe == null)
        {
            bienThe = sp.BienThe.FirstOrDefault(b => b.MacDinh);
        }

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
            TextSearchHelper.NormalizeText(t.Ten).ToLower() ==
            TextSearchHelper.NormalizeText(tenTopping).ToLower());

        return tp?.Id ?? Guid.Empty;
    }

    private decimal GetToppingGia(string tenTopping)
    {
        return _toppingList
            .FirstOrDefault(t =>
                TextSearchHelper.NormalizeText(t.Ten).ToLower() ==
                TextSearchHelper.NormalizeText(tenTopping).ToLower())
            ?.Gia ?? 0;
    }
}

// cookie model
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