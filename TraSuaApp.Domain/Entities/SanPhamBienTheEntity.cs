namespace TraSuaApp.Domain.Entities;

public partial class SanPhamBienThe
{
    public Guid Id { get; set; }

    public string TenBienThe { get; set; } = null!;

    public decimal GiaBan { get; set; }

    public Guid SanPhamId { get; set; }

    public bool MacDinh { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual ICollection<ChiTietHoaDonEntity> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDonEntity>();

    public virtual ICollection<CongThuc> CongThucs { get; set; } = new List<CongThuc>();

    public virtual SanPham SanPham { get; set; } = null!;
    public virtual ICollection<KhachHangGiaBan> KhachHangGiaBans { get; set; } = new List<KhachHangGiaBan>();

}
