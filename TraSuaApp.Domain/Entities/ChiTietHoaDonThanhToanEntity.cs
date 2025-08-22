namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonThanhToan
{
    public Guid Id { get; set; }

    public string LoaiThanhToan { get; set; }
    public Guid? ChiTietHoaDonNoId { get; set; }
    public string? GhiChu { get; set; }

    public string TenPhuongThucThanhToan { get; set; }

    public decimal SoTien { get; set; }
    public DateTime Ngay { get; set; }

    public DateTime NgayGio { get; set; }

    public Guid HoaDonId { get; set; }

    public Guid PhuongThucThanhToanId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }


    public virtual HoaDon HoaDon { get; set; } = null!;
    public virtual ChiTietHoaDonNo ChiTietHoaDonNo { get; set; } = null!;

    public virtual PhuongThucThanhToan PhuongThucThanhToan { get; set; } = null!;
    public Guid? KhachHangId { get; set; }
    public virtual KhachHang? KhachHang { get; set; }
}
