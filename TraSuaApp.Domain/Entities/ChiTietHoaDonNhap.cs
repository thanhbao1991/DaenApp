namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonNhap
{
    public Guid Id { get; set; }

    public Guid IdHoaDonNhap { get; set; }

    public Guid IdNguyenLieu { get; set; }

    public decimal SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public Guid HoaDonNhapId { get; set; }

    public Guid NguyenLieuId { get; set; }

    public virtual HoaDonNhap HoaDonNhap { get; set; } = null!;

    public virtual NguyenLieu NguyenLieu { get; set; } = null!;
}
