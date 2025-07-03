namespace TraSuaApp.Domain.Entities;

public class ChiTietHoaDon
{
    public Guid Id { get; set; }
    public Guid IdHoaDon { get; set; }
    public Guid IdSanPhamBienThe { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
    public int TichDiem { get; set; }

    public HoaDon HoaDon { get; set; }
    public SanPhamBienThe SanPhamBienThe { get; set; }
}
