using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class LichSuChinhSua
{
    public Guid Id { get; set; }

    public DateTime ThoiGian { get; set; }

    public Guid IdTaiKhoan { get; set; }

    public string LoaiThaoTac { get; set; } = null!;

    public string? GhiChu { get; set; }

    public Guid TaiKhoanId { get; set; }

    public virtual TaiKhoan TaiKhoan { get; set; } = null!;
}
