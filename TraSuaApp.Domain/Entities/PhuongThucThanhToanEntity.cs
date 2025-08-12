using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class PhuongThucThanhToan
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public bool DangSuDung { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();
}
