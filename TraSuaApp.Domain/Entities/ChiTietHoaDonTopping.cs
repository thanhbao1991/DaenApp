namespace TraSuaApp.Domain.Entities;

public class ChiTietHoaDonTopping
{
    public Guid Id { get; set; }
    public Guid IdHoaDon { get; set; }
    public Guid IdTopping { get; set; }
    public int SoLuong { get; set; }
    public decimal Gia { get; set; }

    public HoaDon HoaDon { get; set; }
    public Topping Topping { get; set; }
}