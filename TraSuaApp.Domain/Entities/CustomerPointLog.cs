using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class CustomerPointLog
{
    public Guid Id { get; set; }

    public Guid IdKhachHang { get; set; }

    public DateTime ThoiGian { get; set; }

    public int DiemThayDoi { get; set; }

    public string? GhiChu { get; set; }

    public Guid KhachHangId { get; set; }

    public virtual KhachHang KhachHang { get; set; } = null!;
}
