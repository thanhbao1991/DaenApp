using System.Diagnostics;
using System.Drawing.Drawing2D;   // NearestNeighbor
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;             // Clipboard
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using D = System.Drawing;
using DP = System.Drawing.Printing;

namespace TraSuaApp.WpfClient.Helpers
{
    public static class HoaDonPrinter
    {
        // ====== CẤU HÌNH CỐ ĐỊNH ======
        private const string BANK_BIN = "970415";                 // VietinBank (tham khảo cho link)
        private const string BANK_NAME = "VIETINBANK";
        private const string ACCOUNT_NO = "0889664007";
        private const string ACCOUNT_NAME = "TRAN THI HUYEN TRANG";
        // CDN keys ưu tiên cho VietinBank:
        private static readonly string[] _bankKeys = new[] { "ICB", "vietinbank", BANK_BIN };

        // ===== Build full bill (text) =====
        private static string BuildContent(HoaDonDto hoaDon)
        {
            var sb = new StringBuilder();

            // Header
            AddCenterText(sb, "ĐENN", true);
            AddCenterText(sb, "02 Lý Thường Kiệt");
            AddCenterText(sb, "0889 664 007");
            sb.AppendLine("===========================");

            if (hoaDon.KhachHangId != null)
            {
                sb.AppendLine($"Khách hàng: {hoaDon.TenKhachHangText}");
                sb.AppendLine($"{hoaDon.DiaChiText} - {FormatPhone(hoaDon.SoDienThoaiText)}");

                var starText = StarHelper.GetStarText(hoaDon.DiemThangNay);
                if (!string.IsNullOrEmpty(starText))
                    sb.AppendLine($"Điểm tháng này: {starText}");

                var starText2 = StarHelper.GetStarText(hoaDon.DiemThangTruoc);
                if (!string.IsNullOrEmpty(starText2))
                    sb.AppendLine($"Điểm tháng trước: {starText2}");
            }
            sb.AppendLine("---------------------------");
            sb.AppendLine();

            // Chi tiết món
            int stt = 1;
            foreach (var item in hoaDon.ChiTietHoaDons)
            {
                if (item.DonGia <= 0) continue;

                sb.AppendLine($"{stt++}. {item.TenSanPham}" +
                    (!string.IsNullOrEmpty(item.TenBienThe) && item.TenBienThe != "Size Chuẩn"
                        ? $" ({item.TenBienThe})"
                        : ""));

                sb.AppendLine($"   {item.SoLuong} x {item.DonGia:N0} = {item.ThanhTien:N0}");

                if (item.ToppingDtos != null && item.ToppingDtos.Any())
                {
                    foreach (var tp in item.ToppingDtos)
                        sb.AppendLine($"      + {tp.Ten} x{tp.SoLuong} ({tp.Gia:N0})");
                }

                if (!string.IsNullOrWhiteSpace(item.NoteText))
                    sb.AppendLine($"      * {item.NoteText}");

                sb.AppendLine();
            }

            // Footer (đÃ bỏ phần điểm để tránh trùng lặp)
            sb.Append(BuildFooterContent(hoaDon, true));

            // Cảm ơn
            AddCenterText(sb, "Cảm ơn quý khách!");
            AddCenterText(sb, "Hẹn gặp lại ♥");
            sb.AppendLine();

            return sb.ToString();
        }

        public static string BuildFooterContent(HoaDonDto hoaDon, bool includeLine = true)
        {
            var sb = new StringBuilder();

            if (includeLine) sb.AppendLine("---------------------------");

            if (hoaDon.GiamGia > 0)
            {
                AddRow(sb, "TỔNG CỘNG:", hoaDon.TongTien);
                AddRow(sb, "Giảm giá:", hoaDon.GiamGia);
                AddRow(sb, "Thành tiền:", hoaDon.ThanhTien);
            }
            else
            {
                AddRow(sb, "Thành tiền:", hoaDon.ThanhTien);
            }

            if (hoaDon.DaThu > 0)
            {
                AddRow(sb, "Đã thu:", hoaDon.DaThu);
                AddRow(sb, "Còn lại:", hoaDon.ConLai);
            }

            if (hoaDon.TongNoKhachHang > 0)
            {
                if (includeLine) sb.AppendLine("---------------------------");
                AddRow(sb, "Công nợ:", hoaDon.TongNoKhachHang);
                AddRow(sb, "TỔNG:", hoaDon.TongNoKhachHang + hoaDon.ConLai);
            }

            if (includeLine) sb.AppendLine("===========================");
            return sb.ToString();
        }

        // ===== Public: In bill kèm QR ONLINE ở CUỐI (fail -> text-only) =====
        public static void Print(HoaDonDto hoaDon)
        {
            Print(hoaDon, maHoaDon: BuildMaHoaDon(hoaDon), accountName: ACCOUNT_NAME);
        }

        public static void Print(HoaDonDto hoaDon, string maHoaDon, string? accountName)
        {
            var content = BuildContent(hoaDon);
            string printerName = new DP.PrinterSettings().PrinterName;

            decimal amount = GetAmount(hoaDon); // ưu tiên TỔNG nợ nếu có
            string ten = GetTenKhach(hoaDon);
            string addInfo = BuildAddInfo(ten, amount, maHoaDon);

            // thử tải QR ONLINE
            var qrOnline = TryDownloadVietQrOnline(amount, addInfo, accountName ?? ACCOUNT_NAME);

            if (qrOnline != null)
            {
                // in bill + QR ở cuối (canh trái, vừa cột chữ, 2 dòng trống)
                PrintViaGdi(content, printerName, "Consolas", 10f, qrOnline);
            }
            else
            {
                // KHÔNG có QR → chèn block text CK ở CUỐI bill rồi in
                var vi = CultureInfo.GetCultureInfo("vi-VN");
                var sb = new StringBuilder(content);
                sb.AppendLine("---------------------------");
                sb.AppendLine("THÔNG TIN CHUYỂN KHOẢN:");
                sb.AppendLine($"{BANK_NAME} - {ACCOUNT_NO}");
                if (!string.IsNullOrWhiteSpace(accountName ?? ACCOUNT_NAME)) sb.AppendLine(accountName ?? ACCOUNT_NAME);
                sb.AppendLine($"Số tiền: {amount.ToString("#,0₫", vi)}");
                sb.AppendLine($"Nội dung: {addInfo}");
                sb.AppendLine("===========================");

                PrintViaGdi(sb.ToString(), printerName, "Consolas", 10f, qrBmp: null);
            }
        }

        // ======== GỬI ZALO: text hóa đơn + QR ONLINE (nếu lấy được) ========
        public static string BuildBillTextOnly(HoaDonDto hd)
            => BuildContent(hd).TrimEnd();

        public static string? SaveQrOnlineForZalo(HoaDonDto hd, string? path = null)
        {
            decimal amount = GetAmount(hd);
            string ma = BuildMaHoaDon(hd);
            string ten = GetTenKhach(hd);
            string addInfo = BuildAddInfo(ten, amount, ma);

            using var bmp = TryDownloadVietQrOnline(amount, addInfo, ACCOUNT_NAME); // ONLINE ONLY
            if (bmp == null) return null;

            path ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                                  $"QR_{ma}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            bmp.Save(path, ImageFormat.Png);
            return path;
        }

        // gói “luôn copy clipboard”: trả về (đường dẫn ảnh QR nếu có, message)
        public static (string? qrPng, string message) PrepareZaloTextAndQr_AlwaysCopy(HoaDonDto hd, bool openExplorer = true)
        {
            string msg = BuildBillTextOnly(hd);
            string? png = null;

            try { png = SaveQrOnlineForZalo(hd); } catch { png = null; }

            // Nếu không có QR -> bổ sung block CK vào text
            if (png == null)
                msg = AppendTransferBlock(msg, hd);

            // Copy clipboard (best-effort)
            try { Clipboard.SetText(msg); }
            catch
            {
                try { System.Windows.Application.Current?.Dispatcher?.Invoke(() => Clipboard.SetText(msg)); } catch { }
            }

            if (openExplorer && png != null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{png}\"") { UseShellExecute = true });
                }
                catch { }
            }

            return (png, msg);
        }

        private static string AppendTransferBlock(string billText, HoaDonDto hd)
        {
            decimal amount = GetAmount(hd);
            string ma = BuildMaHoaDon(hd);
            string ten = GetTenKhach(hd);
            string addInfo = BuildAddInfo(ten, amount, ma);
            var vi = CultureInfo.GetCultureInfo("vi-VN");

            var sb = new StringBuilder();
            sb.AppendLine(billText);
            sb.AppendLine("---------------------------");
            sb.AppendLine("THÔNG TIN CHUYỂN KHOẢN");
            sb.AppendLine($"{BANK_NAME} - {ACCOUNT_NO}");
            sb.AppendLine(ACCOUNT_NAME);
            sb.AppendLine($"Số tiền: {amount.ToString("#,0₫", vi)}");
            sb.AppendLine($"Nội dung: {addInfo}");
            return sb.ToString().TrimEnd();
        }

        // ===== In text + QR (canh trái, ngang bằng cột chữ, thêm 2 dòng trống) =====
        private static string[]? _lines;
        private static int _lineIndex;
        private static D.Bitmap? _qr;
        private static bool _qrPrinted;

        private static void PrintViaGdi(string content, string printerName, string fontName, float fontSize, D.Bitmap? qrBmp)
        {
            _lines = content.Replace("\r", "").Split('\n');
            _lineIndex = 0;
            _qr = qrBmp;                    // có thể null
            _qrPrinted = (_qr == null);

            var doc = new DP.PrintDocument();
            doc.PrinterSettings.PrinterName = printerName;
            doc.DefaultPageSettings.Margins = new DP.Margins(0, 0, 0, 0);

            doc.PrintPage += (s, e) =>
            {
                var g = e.Graphics;
                g.PageUnit = D.GraphicsUnit.Pixel;

                // MarginBounds -> PX
                int leftPx = (int)Math.Round(e.MarginBounds.Left / 100f * g.DpiX);
                int topPx = (int)Math.Round(e.MarginBounds.Top / 100f * g.DpiY);
                int widthPx = (int)Math.Round(e.MarginBounds.Width / 100f * g.DpiX);
                int bottomPx = (int)Math.Round(e.MarginBounds.Bottom / 100f * g.DpiY);

                using var font = new D.Font(fontName, fontSize, D.FontStyle.Regular, D.GraphicsUnit.Point);
                int lineHpx = (int)Math.Ceiling(font.GetHeight(g));

                g.SmoothingMode = SmoothingMode.None;
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half;

                var fmt = new D.StringFormat(D.StringFormatFlags.NoWrap);
                int y = topPx;

                // 1) In toàn bộ text
                while (_lineIndex < _lines!.Length)
                {
                    if (y + lineHpx > bottomPx) { e.HasMorePages = true; return; }
                    g.DrawString(_lines[_lineIndex++], font, D.Brushes.Black,
                                 new D.RectangleF(leftPx, y, widthPx, lineHpx), fmt);
                    y += lineHpx;
                }

                // 2) In QR ở cuối – ép vừa chiều cao còn lại, thêm 2 dòng trống
                if (!_qrPrinted && _qr != null)
                {
                    int leftPadPx = 0;                                 // sát cột chữ
                    int rightPadPx = 0;
                    int blank2Lines = 2 * lineHpx;                        // thêm 2 dòng trống
                    int safeBottomPx = (int)Math.Round(0.1f * g.DpiY);     // ~2.5mm

                    int maxWidthPx = Math.Max(60, widthPx - leftPadPx - rightPadPx);
                    int maxHeightPx = Math.Max(60, bottomPx - y - safeBottomPx - blank2Lines);

                    int finalMaxPx = Math.Max(80, Math.Min(maxWidthPx, maxHeightPx));

                    int quietPx = Math.Clamp(finalMaxPx / 12, 12, 24);
                    int corePx = Math.Max(64, finalMaxPx - 2 * quietPx);

                    using var crisp = MakeCrispQr(_qr, corePx, extraQuietPx: quietPx);

                    int x = leftPx + leftPadPx;
                    int w = Math.Min(crisp.Width, finalMaxPx);
                    int h = Math.Min(crisp.Height, finalMaxPx);

                    g.DrawImage(crisp, new D.Rectangle(x, y, w, h));
                    y += h + blank2Lines;   // 2 dòng trống để xé giấy

                    _qrPrinted = true;
                }

                e.HasMorePages = false;
            };

            doc.EndPrint += (s, e) => { _qr?.Dispose(); _qr = null; };
            doc.Print();
        }

        // helper phóng nét QR với quiet-zone
        private static D.Bitmap MakeCrispQr(D.Bitmap src, int targetPx, int extraQuietPx = 24)
        {
            var bmp = new D.Bitmap(targetPx + extraQuietPx * 2, targetPx + extraQuietPx * 2, D.Imaging.PixelFormat.Format24bppRgb);
            using var g = D.Graphics.FromImage(bmp);
            g.Clear(D.Color.White);
            g.SmoothingMode = SmoothingMode.None;
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            g.DrawImage(src, new D.Rectangle(extraQuietPx, extraQuietPx, targetPx, targetPx));
            return bmp;
        }

        // ====== QR ONLINE (CDN) ======
        private static string BuildVietQrCdnUrl(string bankKey, decimal amount, string addInfo, string? accountName = ACCOUNT_NAME)
        {
            long vnd = (long)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
            string url = $"https://img.vietqr.io/image/{bankKey}-{ACCOUNT_NO}-qr_only.png" +
                         $"?amount={vnd}&addInfo={Uri.EscapeDataString(addInfo)}&logo=false";
            if (!string.IsNullOrWhiteSpace(accountName))
                url += $"&accountName={Uri.EscapeDataString(accountName!)}";
            return url;
        }

        private static D.Bitmap? TryDownloadVietQrOnline(decimal amount, string addInfo, string? accountName = ACCOUNT_NAME)
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            using var wc = new WebClient();
            foreach (var key in _bankKeys)
            {
                try
                {
                    var url = BuildVietQrCdnUrl(key, amount, addInfo, accountName);
                    byte[] bytes = wc.DownloadData(url);
                    using var ms = new MemoryStream(bytes);
                    return new D.Bitmap(ms);
                }
                catch { /* thử key khác */ }
            }
            return null;
        }

        // ===== Helpers =====
        // Lấy số tiền tạo QR: nếu có công nợ => TỔNG = Công nợ + Còn lại; ngược lại tiền hóa đơn
        private static decimal GetAmount(HoaDonDto hd)
        {
            var bill = hd.ConLai > 0 ? hd.ConLai : hd.ThanhTien;
            if (hd.TongNoKhachHang > 0) return hd.TongNoKhachHang + hd.ConLai; // ConLai=0 nếu đã thu hết
            return bill;
        }

        private static string BuildMaHoaDon(HoaDonDto hd)
            => $"HD{hd.Id.ToString().Substring(0, 8)}";

        private static string GetTenKhach(HoaDonDto hd)
            => !string.IsNullOrWhiteSpace(hd.TenKhachHangText) ? hd.TenKhachHangText : "KHACH";

        // Nội dung: "<ten> thanh toan <so-tien> (HDxxxxxx)"
        private static string BuildAddInfo(string ten, decimal amount, string maHoaDon)
        {
            var vi = CultureInfo.GetCultureInfo("vi-VN");
            string soTien = amount.ToString("#,0", vi); // không kèm đơn vị
            string raw = $"{ten} thanh toan {soTien} ({maHoaDon})";
            return ToAsciiNoDiacritics(raw, upper: true);
        }
        private static string ToAsciiNoDiacritics(string? s, bool upper = true)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            // Khử dấu: Unicode Normalization + bỏ NonSpacingMark
            var norm = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(norm.Length);
            foreach (var ch in norm)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }

            // Chuẩn hóa: Đ/đ -> D/d, gộp khoảng trắng
            var res = sb.ToString().Normalize(NormalizationForm.FormC)
                           .Replace('Đ', 'D').Replace('đ', 'd');
            res = Regex.Replace(res, @"\s+", " ").Trim();

            return upper ? res.ToUpperInvariant() : res;
        }
        private static string FormatPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 10)
                return $"{digits.Substring(0, 4)} {digits.Substring(4, 3)} {digits.Substring(7, 3)}";
            else if (digits.Length == 11)
                return $"{digits.Substring(0, 3)} {digits.Substring(3, 4)} {digits.Substring(7, 4)}";
            else
                return phone;
        }

        private static void AddRow(StringBuilder sb, string left, decimal right, int width = 27)
        {
            string leftPart = left;
            string rightPart = right.ToString("N0");
            int space = width - leftPart.Length - rightPart.Length;
            if (space < 1) space = 1;
            sb.AppendLine(leftPart + new string(' ', space) + rightPart);
        }

        private static void AddCenterText(StringBuilder sb, string text, bool bold = false, int width = 32)
        {
            int space = (width - text.Length) / 2;
            if (space < 0) space = 0;
            sb.AppendLine(new string(' ', space) + text);
        }
    }
}