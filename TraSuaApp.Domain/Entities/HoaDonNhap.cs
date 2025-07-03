namespace TraSuaApp.Domain.Entities;

public class HoaDonNhap
{
    public Guid Id { get; set; }
    public DateTime NgayNhap { get; set; }
    public Guid? IdTaiKhoan { get; set; }
    public string? GhiChu { get; set; }

    public TaiKhoan? TaiKhoan { get; set; }
    public ICollection<ChiTietHoaDonNhap> ChiTietHoaDonNhaps { get; set; }
}