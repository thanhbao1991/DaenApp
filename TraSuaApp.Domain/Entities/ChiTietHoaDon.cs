namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDon
{
    public Guid Id { get; set; }

    public Guid IdHoaDon { get; set; }

    public Guid IdSanPhamBienThe { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public decimal ThanhTien { get; set; }

    public int TichDiem { get; set; }

    public Guid HoaDonId { get; set; }

    public Guid SanPhamBienTheId { get; set; }

    public virtual HoaDon HoaDon { get; set; } = null!;

    public virtual SanPhamBienThe SanPhamBienThe { get; set; } = null!;
}
