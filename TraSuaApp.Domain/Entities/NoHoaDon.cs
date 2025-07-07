using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class NoHoaDon
{
    public Guid Id { get; set; }

    public Guid IdHoaDon { get; set; }

    public decimal SoTienNo { get; set; }

    public decimal SoTienDaTra { get; set; }

    public DateTime NgayGhiNhan { get; set; }

    public Guid HoaDonId { get; set; }

    public virtual HoaDon HoaDon { get; set; } = null!;
}
