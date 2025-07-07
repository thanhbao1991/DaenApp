using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonTopping
{
    public Guid Id { get; set; }

    public Guid IdHoaDon { get; set; }

    public Guid IdTopping { get; set; }

    public int SoLuong { get; set; }

    public decimal Gia { get; set; }

    public Guid HoaDonId { get; set; }

    public Guid ToppingId { get; set; }

    public virtual HoaDon HoaDon { get; set; } = null!;

    public virtual Topping Topping { get; set; } = null!;
}
