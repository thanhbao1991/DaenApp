using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class Voucher
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public decimal GiaTri { get; set; }

    public DateTime NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public bool DangSuDung { get; set; }

    public virtual ICollection<VoucherLog> VoucherLogs { get; set; } = new List<VoucherLog>();
}
