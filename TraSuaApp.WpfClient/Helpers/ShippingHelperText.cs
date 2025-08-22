using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

public class AppShippingHelperText
{
    private readonly string _username;
    private readonly string _password;
    private readonly string _cookieFile = "cookies.json";

    // --- XPath Login ---
    private readonly string usernameXPath = "//*[@id='app']/div/form/div[2]/div/div[1]/input";
    private readonly string passwordXPath = "//*[@id='app']/div/form/div[3]/div/div[1]/input";
    private readonly string loginButtonXPath = "/html/body/div/div/form/button";

    // --- XPath Kiểm tra Login thành công ---
    private readonly string avatarXPath = "//*[@id=\"app\"]/div/div[2]/div/div/div[3]/div/div/img";

    // --- URL Order Page ---
    private readonly string orderPageUrl = "https://store.shippershipping.com/#/store/order";

    // --- XPath đơn hàng ---
    private readonly string TrangThaiRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[2]/div/span";
    private readonly string TenTaiXeRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[3]/div/ul/li/span";
    private readonly string TenKhachHangRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[4]/div/ul/li[1]/span";
    private readonly string DiaChiRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[4]/div/ul/li[2]/span";
    private readonly string XemChiTietRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[5]/div/div[1]/a/span";
    //private readonly string XemChiTietRow2XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[3]/td[5]/div/div[1]/a/span";
    //private readonly string XemChiTietRow3XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[4]/td[5]/div/div[1]/a/span";
    private readonly string CodeRow1XPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[3]/div[3]/table/tbody/tr[1]/td[2]/div/p";

    private readonly string ChiTietPopupXPath = "//*[@id=\"app\"]/div/div[2]/section/div/div[6]/div";

    public AppShippingHelperText(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public HoaDonAppDto GetFirstOrderPopup()

    {
        var options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--disable-gpu");
        options.AddArgument("--window-size=1920,1080");

        // using var driver = new ChromeDriver(options);
        var service = ChromeDriverService.CreateDefaultService();
        service.HideCommandPromptWindow = true;
        service.SuppressInitialDiagnosticInformation = true;

        using var driver = new ChromeDriver(service, options);
        WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(20));

        try
        {
            driver.Navigate().GoToUrl("https://store.shippershipping.com");

            // Load cookie nếu có
            if (File.Exists(_cookieFile))
            {
                var json = File.ReadAllText(_cookieFile);
                var cookies = JsonSerializer.Deserialize<List<CookieData>>(json);
                foreach (var c in cookies)
                {
                    if (string.IsNullOrEmpty(c.Name) || string.IsNullOrEmpty(c.Value))
                        continue;

                    driver.Manage().Cookies.AddCookie(new Cookie(
                        c.Name,
                        c.Value,
                        c.Domain,
                        c.Path,
                        c.Expiry
                    ));
                }

                driver.Navigate().Refresh();
                wait.Until(d => d.FindElements(By.XPath(avatarXPath)).Count > 0);
            }

            // Login nếu chưa đăng nhập
            if (!IsLoggedIn(driver))
            {
                Login(driver, wait);
            }

            driver.Navigate().GoToUrl(orderPageUrl);

            // Chờ bảng load xong (element đầu tiên xuất hiện)
            var firstOrderRow = wait.Until(d =>
            {
                var elems = d.FindElements(By.XPath(DiaChiRow1XPath));
                return (elems.Count > 0 && elems[0].Displayed) ? elems[0] : null;
            });
            // Lấy địa chỉ từ cột
            string trangThai = firstOrderRow.FindElement(By.XPath(TrangThaiRow1XPath)).Text;
            string tenTaiXe = firstOrderRow.FindElement(By.XPath(TenTaiXeRow1XPath)).Text;
            string TenKhachHang = firstOrderRow.FindElement(By.XPath(TenKhachHangRow1XPath)).Text;
            string diachiText = firstOrderRow.FindElement(By.XPath(DiaChiRow1XPath)).Text;
            string codeText = firstOrderRow.FindElement(By.XPath(CodeRow1XPath)).Text;

            // Chờ nút click khả dụng
            var firstOrderClick = wait.Until(d =>
            {
                var elem = firstOrderRow.FindElement(By.XPath(XemChiTietRow1XPath));
                return (elem.Displayed && elem.Enabled) ? elem : null;
            });

            firstOrderClick.Click();

            // Chờ popup hiện
            //var popup = wait.Until(d =>
            //{
            //    var elems = d.FindElements(By.XPath(ChiTietPopupXPath));
            //    return (elems.Count > 0 && elems[0].Displayed) ? elems[0] : null;
            //});
            //string popupText = popup.Text;


            // Chờ popup hiện
            // --- Trong AppShippingHelperText sau khi mở popup ---
            var popup = wait.Until(d =>
            {
                var elems = d.FindElements(By.XPath(ChiTietPopupXPath));
                return (elems.Count > 0 && elems[0].Displayed) ? elems[0] : null;
            });

            // Lấy từng item món
            var itemElements = popup.FindElements(By.CssSelector("ul.food-list > li.food-item"));
            var chiTietDonHang = new List<ChiTietHoaDonAppDto>();

            foreach (var item in itemElements)
            {
                var dto = new ChiTietHoaDonAppDto();

                // Tên món
                var nameElem = item.FindElement(By.CssSelector("div.product-name > span.name"));
                dto.TenSanPham = nameElem.Text.Trim();

                // Số lượng tổng món (quantity-badge)
                var qtyElem = item.FindElement(By.CssSelector("div.product-order > span.quantity-badge"));
                string qtyText = qtyElem.Text.Trim().Replace("x", "").Trim();
                dto.SoLuong = int.TryParse(qtyText, out int qty) ? qty : 1;

                // Giá đơn món
                var singlePriceElem = item.FindElement(By.CssSelector("div.product-name span.single-price"));
                string priceText = singlePriceElem.Text.Replace(",", "").Trim();
                dto.DonGia = decimal.TryParse(priceText, out decimal price) ? price : 0;

                // Giá tổng của món
                var totalPriceElem = item.FindElement(By.CssSelector("div > span.price"));
                string totalText = totalPriceElem.Text.Replace(",", "").Trim();
                dto.TongTien = decimal.TryParse(totalText, out decimal total) ? total : 0;

                // Options chi tiết
                var optionDivs = item.FindElements(By.CssSelector("div.product-name > div > div"));
                foreach (var optDiv in optionDivs)
                {
                    var spans = optDiv.FindElements(By.TagName("span"));
                    if (spans.Count >= 2)
                    {
                        string optionName = spans[0].Text.Trim();
                        string optionQtyText = spans[1].Text.Trim().Replace("x", "").Trim();
                        if (int.TryParse(optionQtyText, out int optionQty))
                        {
                            dto.Options.Add(new SanPhamOption
                            {
                                TenOption = optionName,
                                SoLuong = optionQty
                            });
                        }
                    }
                }

                chiTietDonHang.Add(dto);
            }
            return new HoaDonAppDto
            {
                TenKhachHang = TenKhachHang,
                TenTaiXe = tenTaiXe,
                TrangThai = trangThai,
                Code = codeText,
                DiaChi = diachiText,
                ChiTietDonHang = chiTietDonHang
            };

        }
        finally
        {
            driver.Quit();
        }
    }

    private bool IsLoggedIn(IWebDriver driver)
    {
        return driver.FindElements(By.XPath(avatarXPath)).Count > 0;
    }

    private void Login(IWebDriver driver, WebDriverWait wait)
    {
        driver.FindElement(By.XPath(usernameXPath)).SendKeys(_username);
        driver.FindElement(By.XPath(passwordXPath)).SendKeys(_password);
        driver.FindElement(By.XPath(loginButtonXPath)).Click();

        // Chờ avatar xuất hiện xác nhận login thành công
        wait.Until(d => d.FindElements(By.XPath(avatarXPath)).Count > 0);

        // Lưu cookie
        var cookies = driver.Manage().Cookies.AllCookies;
        File.WriteAllText(_cookieFile, JsonSerializer.Serialize(cookies));
    }
}

// --- DTO chi tiết món ---
public class HoaDonAppDto
{
    public string TenTaiXe { get; set; }
    public string TenKhachHang { get; set; }
    public string Code { get; set; }
    public string DiaChi { get; set; }
    public List<ChiTietHoaDonAppDto> ChiTietDonHang { get; set; } = new();
    public string TrangThai { get; internal set; }
}

public class ChiTietHoaDonAppDto
{
    public string TenSanPham { get; set; }
    public int SoLuong { get; set; }           // Tổng số lượng món (quantity-badge)
    public decimal DonGia { get; set; }        // Giá một món
    public decimal TongTien { get; set; }      // Giá tổng của món
    public List<SanPhamOption> Options { get; set; } = new();

}

public class SanPhamOption
{
    public string TenOption { get; set; }
    public int SoLuong { get; set; }
}
public class CookieData
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    [JsonPropertyName("domain")]
    public string Domain { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("expiry")]
    public DateTime? Expiry { get; set; }

    [JsonPropertyName("secure")]
    public bool Secure { get; set; }

    [JsonPropertyName("httpOnly")]
    public bool HttpOnly { get; set; }

    [JsonPropertyName("sameSite")]
    public string SameSite { get; set; }
}