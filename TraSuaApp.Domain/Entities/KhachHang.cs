using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class KhachHang
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public DateTime? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public bool DuocNhanVoucher { get; set; }

    public virtual ICollection<CustomerPointLog> CustomerPointLogs { get; set; } = new List<CustomerPointLog>();

    public virtual ICollection<CustomerPoint> CustomerPoints { get; set; } = new List<CustomerPoint>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<KhachHangAddress> KhachHangAddresses { get; set; } = new List<KhachHangAddress>();

    public virtual ICollection<KhachHangPhone> KhachHangPhones { get; set; } = new List<KhachHangPhone>();
}
