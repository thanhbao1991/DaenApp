using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class VoucherLog
{
    public Guid Id { get; set; }

    public Guid IdVoucher { get; set; }

    public Guid IdHoaDon { get; set; }

    public decimal GiaTriApDung { get; set; }

    public Guid VoucherId { get; set; }

    public Guid HoaDonId { get; set; }

    public virtual HoaDon HoaDon { get; set; } = null!;

    public virtual Voucher Voucher { get; set; } = null!;
}
