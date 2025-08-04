using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class LichSuNhapXuatKho
{
    public Guid Id { get; set; }

    public DateTime ThoiGian { get; set; }

    public Guid IdNguyenLieu { get; set; }

    public decimal SoLuong { get; set; }

    public string Loai { get; set; } = null!;

    public string? GhiChu { get; set; }

    public Guid NguyenLieuId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual NguyenLieu NguyenLieu { get; set; } = null!;
}
