namespace TraSuaApp.Domain.Entities;

public partial class KhachHang : EntityBase
{
    public int? OldId { get; set; }

    public string Ten { get; set; } = null!;

    public bool DuocNhanVoucher { get; set; }

    public virtual ICollection<CustomerPointLog> CustomerPointLogs { get; set; } = new List<CustomerPointLog>();

    public virtual ICollection<CustomerPoint> CustomerPoints { get; set; } = new List<CustomerPoint>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<KhachHangAddress> KhachHangAddresses { get; set; } = new List<KhachHangAddress>();

    public virtual ICollection<KhachHangPhone> KhachHangPhones { get; set; } = new List<KhachHangPhone>();
}
