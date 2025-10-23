using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class KhachHangDto : DtoBase
{
    public override string ApiRoute => "KhachHang";
    public int? OldId { get; set; }
    public int ThuTu { get; set; }
    public bool DuocNhanVoucher { get; set; }
    public virtual List<KhachHangPhoneDto> Phones { get; set; } = new List<KhachHangPhoneDto>();
    public virtual List<KhachHangAddressDto> Addresses { get; set; } = new List<KhachHangAddressDto>();
    public string? DienThoai => Phones.FirstOrDefault(p => p.IsDefault)?.SoDienThoai ?? string.Empty;
    public string? DiaChi => Addresses.FirstOrDefault(a => a.IsDefault)?.DiaChi ?? string.Empty;


    public string TimKiem =>
    $"{Ten?.ToLower() ?? ""} " +
    $"{StringHelper.MyNormalizeText(Ten ?? "")} " +
    $"{StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", ""))} " +
    $"{StringHelper.GetShortName(Ten ?? "")} " +
    $"{DiaChi?.ToLower() ?? ""} " +
    $"{StringHelper.MyNormalizeText(DiaChi ?? "")} " +
    $"{StringHelper.MyNormalizeText((DiaChi ?? "").Replace(" ", ""))} " +
    $"{StringHelper.GetShortName(DiaChi ?? "")} " +
    $"{StringHelper.MyNormalizeText(DienThoai ?? "")}";

    public string? FavoriteMon { get; set; }

    // 🟟 Danh sách địa chỉ



    public override string ToString()
    {
        return $"{Ten}";
    }

}

