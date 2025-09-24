namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonNo
{
    public Guid Id { get; set; }

    public decimal SoTienNo { get; set; }

    public decimal SoTienConLai { get; set; }
    public string? GhiChu { get; set; }


    public DateTime NgayGio { get; set; }

    public Guid HoaDonId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public DateTime Ngay { get; set; }

    public Guid? KhachHangId { get; set; }
    // public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();

    public virtual HoaDon HoaDon { get; set; } = null!;
    public KhachHang KhachHang { get; set; }
}
