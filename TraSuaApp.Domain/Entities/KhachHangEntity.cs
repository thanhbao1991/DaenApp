namespace TraSuaApp.Domain.Entities;

public partial class KhachHang
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public bool DuocNhanVoucher { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int? OldId { get; set; }

    public virtual ICollection<ChiTietHoaDonPoint> ChiTietHoaDonPoints { get; set; } = new List<ChiTietHoaDonPoint>();


    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<KhachHangAddress> KhachHangAddresses { get; set; } = new List<KhachHangAddress>();

    public virtual ICollection<KhachHangPhone> KhachHangPhones { get; set; } = new List<KhachHangPhone>();
}
