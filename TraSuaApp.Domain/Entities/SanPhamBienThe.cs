using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class SanPhamBienThe
{
    public Guid Id { get; set; }

    public Guid IdSanPham { get; set; }

    public string TenBienThe { get; set; } = null!;

    public decimal GiaBan { get; set; }

    public Guid SanPhamId { get; set; }

    public bool MacDinh { get; set; }

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual ICollection<CongThuc> CongThucs { get; set; } = new List<CongThuc>();

    public virtual SanPham SanPham { get; set; } = null!;
}
