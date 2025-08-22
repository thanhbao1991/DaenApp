using System.Drawing.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Helpers
{
    public static class HoaDonPrinter
    {
        private static string BuildContent(HoaDonDto hoaDon)
        {
            StringBuilder sb = new StringBuilder();

            // ===== Header =====
            AddCenterText(sb, "ĐENN", true);
            AddCenterText(sb, "02 Lý Thường Kiệt");
            AddCenterText(sb, "0889 664 007");
            sb.AppendLine("===========================");

            if (hoaDon.KhachHangId != null)
            {
                sb.AppendLine($"Khách hàng: {hoaDon.TenKhachHangText} - {hoaDon.DiaChiText} - {FormatPhone(hoaDon.SoDienThoaiText)}");

                // ⭐ Sao theo điểm
                decimal stars = hoaDon.DiemTrongThang / 3000m;
                int fullStars = (int)Math.Floor(stars);
                bool halfStar = (stars - fullStars) >= 0.5m;

                string starIcons = "";
                if (hoaDon.DiemTrongThang < 3000)
                    starIcons = "☆";
                else
                {
                    starIcons = new string('★', fullStars);
                    if (halfStar) starIcons += "☆";
                }

                sb.AppendLine($"Điểm: {starIcons} ({hoaDon.DiemTrongThang / 10})");
            }
            sb.AppendLine("---------------------------");
            sb.AppendLine();

            // ===== Chi tiết =====
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
                    {
                        sb.AppendLine($"      + {tp.Ten} x{tp.SoLuong} ({tp.Gia:N0})");
                    }
                }

                if (!string.IsNullOrWhiteSpace(item.NoteText))
                {
                    sb.AppendLine($"      * {item.NoteText}");
                }

                sb.AppendLine();
            }

            // ===== Footer =====
            sb.AppendLine("---------------------------");
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
                sb.AppendLine("---------------------------");
                AddRow(sb, "* CÔNG NỢ:", hoaDon.TongNoKhachHang);
            }
            sb.AppendLine("===========================");

            AddCenterText(sb, "Cảm ơn quý khách!");
            AddCenterText(sb, "Hẹn gặp lại ♥");
            sb.AppendLine();

            return sb.ToString();
        }

        // ===== Public Methods =====

        public static void Print(HoaDonDto hoaDon)
        {
            var content = BuildContent(hoaDon);
            string defaultPrinter = new PrinterSettings().PrinterName;
            RawPrinterHelper.SendStringToPrinter(defaultPrinter, content);
        }

        public static void Copy(HoaDonDto hoaDon)
        {
            var content = BuildContent(hoaDon);
            Clipboard.SetText(content);
        }

        public static void Preview(HoaDonDto hoaDon, Window owner)
        {
            var content = BuildContent(hoaDon);

            var tb = new TextBox
            {
                Text = content,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 13,
                Height = 400,

                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            var wnd = new Window
            {
                Title = "Xem trước hóa đơn",
                Owner = owner,
                Width = 300,
                ResizeMode = ResizeMode.NoResize,           // ❌ bỏ Min/Max
                WindowStyle = WindowStyle.SingleBorderWindow,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Content = tb
            };

            wnd.ShowDialog();
        }

        // ===== Helpers =====

        private static string FormatPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return "";

            var digits = new string(phone.Where(char.IsDigit).ToArray());

            if (digits.Length == 10)
                return $"{digits.Substring(0, 4)} {digits.Substring(4, 3)} {digits.Substring(7, 3)}";
            else if (digits.Length == 11)
                return $"{digits.Substring(0, 3)} {digits.Substring(3, 4)} {digits.Substring(7, 4)}";
            else
                return phone;
        }

        private static void AddRow(StringBuilder sb, string left, decimal right, int width = 30)
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