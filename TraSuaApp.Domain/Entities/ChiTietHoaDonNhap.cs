namespace TraSuaApp.Domain.Entities;

public class ChiTietHoaDonNhap
{
    public Guid Id { get; set; }
    public Guid IdHoaDonNhap { get; set; }
    public Guid IdNguyenLieu { get; set; }
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }

    public HoaDonNhap HoaDonNhap { get; set; }
    public NguyenLieu NguyenLieu { get; set; }
}