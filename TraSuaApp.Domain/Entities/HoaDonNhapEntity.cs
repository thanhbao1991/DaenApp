using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class HoaDonNhap
{
    public Guid Id { get; set; }

    public DateTime NgayNhap { get; set; }

    public string? GhiChu { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual ICollection<ChiTietHoaDonNhapEntity> ChiTietHoaDonNhaps { get; set; } = new List<ChiTietHoaDonNhapEntity>();
}
