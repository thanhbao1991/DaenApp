namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonNhapEntity
{
    public Guid Id { get; set; }

    public decimal SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public Guid HoaDonNhapId { get; set; }

    public Guid NguyenLieuId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual HoaDonNhap HoaDonNhap { get; set; } = null!;

    public virtual NguyenLieu NguyenLieu { get; set; } = null!;
}
