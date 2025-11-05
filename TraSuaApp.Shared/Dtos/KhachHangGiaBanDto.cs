using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class KhachHangGiaBanDto : DtoBase
{
    public override string ApiRoute => "KhachHangGiaBan";

    public Guid KhachHangId { get; set; }
    public Guid SanPhamBienTheId { get; set; }
    public decimal GiaBan { get; set; }

    // Thông tin hiển thị
    public string? TenKhachHang { get; set; }
    public string? TenSanPham { get; set; }
    public string? TenBienThe { get; set; }

    // Tìm kiếm theo KH/SP/Biến thể/Giá
    public string TimKiem
    {
        get
        {
            var kh = TenKhachHang ?? "";
            var sp = TenSanPham ?? "";
            var bt = TenBienThe ?? "";
            var gia = GiaBan.ToString();
            var joined = $"{kh} {sp} {bt} {gia}";

            return string.Join(' ',
                (joined).ToLower(),
                StringHelper.MyNormalizeText(joined),
                StringHelper.MyNormalizeText(joined.Replace(" ", "")),
                StringHelper.GetShortName(joined)
            );
        }
    }
}