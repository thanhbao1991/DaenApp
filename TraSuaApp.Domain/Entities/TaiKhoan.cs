namespace TraSuaApp.Domain.Entities;

public partial class TaiKhoan
{

    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Guid Id { get; set; }

    public string TenDangNhap { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public string? VaiTro { get; set; }

    public bool IsActive { get; set; }

    public string? TenHienThi { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public virtual ICollection<CongViecNoiBo> CongViecNoiBos { get; set; } = new List<CongViecNoiBo>();

    public virtual ICollection<HoaDonNhap> HoaDonNhaps { get; set; } = new List<HoaDonNhap>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<LichSuChinhSua> LichSuChinhSuas { get; set; } = new List<LichSuChinhSua>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
