using TraSuaApp.Shared.Helpers;
namespace TraSuaApp.Shared.Dtos;

public class KhachHangDto : DtoBase
{
    public override string ApiRoute => "KhachHang";
    public int? OldId { get; set; }

    public bool DuocNhanVoucher { get; set; }

    public virtual List<KhachHangPhoneDto> Phones { get; set; } = new List<KhachHangPhoneDto>();
    public virtual List<KhachHangAddressDto> Addresses { get; set; } = new List<KhachHangAddressDto>();
    public override string TimKiem
    {
        get
        {
            var allPhones = Phones != null && Phones.Count > 0
                ? string.Join(" ", Phones.Select(p => p.SoDienThoai))
                : string.Empty;

            var parts = new List<string?>
        {
            Ten,
            Ten?.Replace(" ", ""),
            TextSearchHelper.GetShortName(Ten ?? ""),
            DiaChi,
            DiaChi?.Replace(" ", ""),
            TextSearchHelper.GetShortName(DiaChi ?? ""),
            allPhones,                       // ✅ Thêm tất cả SĐT
            allPhones.Replace(" ", "")       // ✅ Thêm dạng không dấu cách
        };

            var rawText = string.Join(" ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
            return TextSearchHelper.NormalizeText(rawText);
        }
    }
    public string? DienThoai => Phones.FirstOrDefault(p => p.IsDefault)?.SoDienThoai ?? string.Empty;
    public string? DiaChi => Addresses.FirstOrDefault(a => a.IsDefault)?.DiaChi ?? string.Empty;
    // 🟟 Danh sách địa chỉ



    public override string ToString()
    {
        return $"{Ten}";
    }

}

