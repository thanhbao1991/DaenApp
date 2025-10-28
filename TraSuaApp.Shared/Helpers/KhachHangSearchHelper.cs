using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Shared.Helpers
{
    public static class KhachHangSearchHelper
    {
        public static string BuildTimKiem(KhachHangDto dto)
        {
            // ===== Name =====
            var rawName = dto.Ten?.Trim() ?? string.Empty;
            var nameNx = StringHelper.MyNormalizeText(rawName);
            var nameSpc = nameNx;                 // "bao thanh"
            var nameCmp = nameNx.Replace(" ", "");// "baothanh"
            var initials = string.Join("", nameNx
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w[0]));              // "bt"

            // ===== Phones =====
            var phones = (dto.Phones ?? new List<KhachHangPhoneDto>())
                .OrderByDescending(x => x.IsDefault)
                .Take(3)
                .Select(x => x.SoDienThoai ?? "")
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct()
                .ToList();

            // ===== Address (default rồi fallback cái đầu) =====
            var addrRaw = (dto.Addresses ?? new List<KhachHangAddressDto>())
                .OrderByDescending(x => x.IsDefault)
                .Select(x => x.DiaChi ?? "")
                .FirstOrDefault() ?? string.Empty;

            var addrNx = StringHelper.MyNormalizeText(addrRaw);   // "02 ly thuong kiet"
            var addrSpc = addrNx;
            var addrCmp = addrNx.Replace(" ", "");                 // "02lythuongkiet"

            // Tokens "02ltk" + "ltk"
            string house = "", streetInitials = "";
            if (!string.IsNullOrWhiteSpace(addrNx))
            {
                int i = 0;
                while (i < addrNx.Length && char.IsDigit(addrNx[i])) i++;
                house = addrNx.Substring(0, i).Trim();             // "02"
                var street = addrNx.Substring(i).Trim();           // "ly thuong kiet"
                streetInitials = string.Join("", street
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => w[0]));                           // "ltk"
            }
            var addrTok = (!string.IsNullOrEmpty(house) && !string.IsNullOrEmpty(streetInitials))
                ? house + streetInitials                            // "02ltk"
                : string.Empty;

            // ===== Merge tokens (thứ tự ưu tiên) =====
            var tokens = new List<string>();
            if (!string.IsNullOrEmpty(nameCmp)) tokens.Add(nameCmp);        // baothanh
            if (!string.IsNullOrEmpty(initials)) tokens.Add(initials);      // bt
            if (!string.IsNullOrEmpty(nameSpc)) tokens.Add(nameSpc);        // bao thanh

            tokens.AddRange(phones);                                        // 0905...

            if (!string.IsNullOrEmpty(addrCmp)) tokens.Add(addrCmp);        // 02lythuongkiet
            if (!string.IsNullOrEmpty(addrSpc)) tokens.Add(addrSpc);        // 02 ly thuong kiet
            if (!string.IsNullOrEmpty(addrTok)) tokens.Add(addrTok);        // 02ltk
            if (!string.IsNullOrEmpty(streetInitials)) tokens.Add(streetInitials); // ltk

            var finalTokens = tokens
                .Select(t => t.Trim())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .ToList();

            var joined = string.Join(';', finalTokens);
            return string.IsNullOrEmpty(joined) ? string.Empty : (joined + ';');
        }
    }
}