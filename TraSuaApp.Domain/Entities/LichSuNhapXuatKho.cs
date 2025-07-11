﻿using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class LichSuNhapXuatKho
{
    public Guid Id { get; set; }

    public DateTime ThoiGian { get; set; }

    public Guid IdNguyenLieu { get; set; }

    public decimal SoLuong { get; set; }

    public string Loai { get; set; } = null!;

    public string? GhiChu { get; set; }

    public Guid NguyenLieuId { get; set; }

    public virtual NguyenLieu NguyenLieu { get; set; } = null!;
}
