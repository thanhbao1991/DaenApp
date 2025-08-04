using System.Globalization;
using System.Text;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Shared.Helpers
{
    public static class TextSearchHelper
    {
        /// <summary>
        /// Chuyển đổi chuỗi thành dạng không dấu, chữ thường và thay đổi 'đ' thành 'd'.
        /// </summary>
        public static string NormalizeText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var normalized = input.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }
            return builder.ToString()
                .Normalize(NormalizationForm.FormC)
                .ToLowerInvariant()
                .Replace("đ", "d");
        }
        public static string GetShortName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string normalized = NormalizeText(input);
            var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var shortName = new StringBuilder();

            foreach (var word in words)
            {
                if (string.IsNullOrWhiteSpace(word))
                    continue;

                // Nếu từ bắt đầu bằng chữ + số (vd: 5b, 3a) → lấy toàn bộ
                if (char.IsDigit(word[0]) || (word.Length > 1 && char.IsDigit(word[1])))
                {
                    shortName.Append(word);
                }
                else
                {
                    shortName.Append(word[0]);
                }
            }

            return shortName.ToString().ToLower();
        }
        public static string GetAcronym(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";
            var words = NormalizeText(input).Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var acronym = new StringBuilder();
            foreach (var word in words)
            {
                if (!string.IsNullOrWhiteSpace(word))
                    acronym.Append(word[0]);
            }
            return acronym.ToString(); // không cần .ToLowerInvariant() nữa vì Normalize đã làm
        }

        public static bool IsMatch(string keyword, string text)
        {
            var normalizedKeyword = NormalizeText(keyword);
            var normalizedText = NormalizeText(text);
            var acronymText = GetAcronym(text);

            return normalizedText.Contains(normalizedKeyword) || acronymText.Contains(normalizedKeyword);
        }

        public static List<SanPhamDto> FilterSanPhams(List<SanPhamDto> allProducts, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return allProducts;

            return allProducts.Where(x =>
                    IsMatch(keyword, x.Ten) ||
                      (!string.IsNullOrEmpty(x.TenNhomSanPham) && NormalizeText(x.TenNhomSanPham).Contains(NormalizeText(keyword))) ||
                    (!string.IsNullOrEmpty(x.VietTat) && NormalizeText(x.VietTat).Contains(NormalizeText(keyword)))
            ).ToList();
        }

        public static List<TaiKhoanDto> FilterTaiKhoans(List<TaiKhoanDto> allAccounts, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return allAccounts;

            return allAccounts.Where(x =>
                    IsMatch(keyword, x.TenDangNhap) ||
                    (!string.IsNullOrWhiteSpace(x.TenHienThi) && IsMatch(keyword, x.TenHienThi))
            ).ToList();
        }

        public static List<T> FilterByTen<T>(List<T> items, string keyword, Func<T, string?> getText)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return items;

            return items
                .Where(x => !string.IsNullOrWhiteSpace(getText(x)) && IsMatch(keyword, getText(x)!))
                .ToList();
        }
    }
}
