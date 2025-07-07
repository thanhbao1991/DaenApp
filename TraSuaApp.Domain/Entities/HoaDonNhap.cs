using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class HoaDonNhap
{
    public Guid Id { get; set; }

    public DateTime NgayNhap { get; set; }

    public Guid? IdTaiKhoan { get; set; }

    public string? GhiChu { get; set; }

    public Guid? TaiKhoanId { get; set; }

    public virtual ICollection<ChiTietHoaDonNhap> ChiTietHoaDonNhaps { get; set; } = new List<ChiTietHoaDonNhap>();

    public virtual TaiKhoan? TaiKhoan { get; set; }
}
