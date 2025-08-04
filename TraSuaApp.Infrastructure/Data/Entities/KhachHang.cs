using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

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

    public virtual ICollection<DiemKhachHangLog> DiemKhachHangLogs { get; set; } = new List<DiemKhachHangLog>();

    public virtual ICollection<DiemKhachHang> DiemKhachHangs { get; set; } = new List<DiemKhachHang>();

    public virtual ICollection<HoaDon> HoaDons { get; set; } = new List<HoaDon>();

    public virtual ICollection<KhachHangAddress> KhachHangAddresses { get; set; } = new List<KhachHangAddress>();

    public virtual ICollection<KhachHangPhone> KhachHangPhones { get; set; } = new List<KhachHangPhone>();
}
