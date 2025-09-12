using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class KhachHangGiaBanDto : DtoBase
{
    public override string ApiRoute => "KhachHangGiaBan";

    public Guid KhachHangId { get; set; }
    public Guid SanPhamBienTheId { get; set; }
    public decimal GiaBan { get; set; }

    // Thông tin hiển thị (optional)
    public string? TenKhachHang { get; set; }
    public string? TenSanPham { get; set; }
    public string? TenBienThe { get; set; }
    public string TimKiem =>
    $"{Ten?.ToLower() ?? ""} " +
    TextSearchHelper.NormalizeText(Ten ?? "") + " " +
    TextSearchHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
    TextSearchHelper.GetShortName(Ten ?? "");

}

