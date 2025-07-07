using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class NguyenLieu
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public string? DonViTinh { get; set; }

    public decimal TonKho { get; set; }

    public decimal? GiaNhap { get; set; }

    public bool DangSuDung { get; set; }

    public virtual ICollection<ChiTietHoaDonNhap> ChiTietHoaDonNhaps { get; set; } = new List<ChiTietHoaDonNhap>();

    public virtual ICollection<LichSuNhapXuatKho> LichSuNhapXuatKhos { get; set; } = new List<LichSuNhapXuatKho>();

    public virtual ICollection<SuDungNguyenLieu> SuDungNguyenLieus { get; set; } = new List<SuDungNguyenLieu>();
}
