using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class ChiTietHoaDonNo
{
    public Guid Id { get; set; }

    public decimal SoTienNo { get; set; }

    public decimal SoTienDaTra { get; set; }

    public DateTime NgayGio { get; set; }

    public Guid HoaDonId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public DateTime Ngay { get; set; }

    public Guid? KhachHangId { get; set; }

    public virtual HoaDon HoaDon { get; set; } = null!;
}
