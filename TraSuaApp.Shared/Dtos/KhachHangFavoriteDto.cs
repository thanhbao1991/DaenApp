namespace TraSuaApp.Shared.Dtos;


public class KhachHangFavoriteDto
{
    public Guid KhachHangId { get; set; }
    public bool DuocNhanVoucher { get; set; }
    public bool DaNhanVoucher { get; set; }
    public int DiemThangNay { get; set; }
    public int DiemThangTruoc { get; set; }
    public decimal TongNo { get; set; }
    public List<ChiTietHoaDonDto> TopChiTiets { get; set; } = new();
}
