using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class HoaDonNhap
{
    public Guid Id { get; set; }

    public DateTime NgayNhap { get; set; }

    public string? GhiChu { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual ICollection<ChiTietHoaDonNhap> ChiTietHoaDonNhaps { get; set; } = new List<ChiTietHoaDonNhap>();
}
