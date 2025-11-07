namespace TraSuaApp.Shared.Dtos;


public class KhachHangFavoriteDto
{
    public Guid KhachHangId { get; set; }
    public bool DuocNhanVoucher { get; set; }
    public bool DaNhanVoucher { get; set; }
    public int DiemThangNay { get; set; }
    public int DiemThangTruoc { get; set; }
    public decimal TongNo { get; set; }
    public decimal DonKhac { get; set; }

    // 🟟 Favorite dựa trên hoá đơn chỉ có 1 món và SUM(SoLuong)=1 trong năm nay
    public string? MonYeuThich { get; set; }                // Ten sp
}
