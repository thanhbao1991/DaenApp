using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class ChiTietHoaDonVoucher
{
    public Guid Id { get; set; }

    public decimal GiaTriApDung { get; set; }

    public Guid VoucherId { get; set; }

    public Guid HoaDonId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public string TenVoucher { get; set; } = null!;

    public virtual HoaDon HoaDon { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
