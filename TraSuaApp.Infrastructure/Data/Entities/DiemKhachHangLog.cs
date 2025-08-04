using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class DiemKhachHangLog
{
    public Guid Id { get; set; }

    public DateTime ThoiGian { get; set; }

    public int DiemThayDoi { get; set; }

    public string? GhiChu { get; set; }

    public Guid KhachHangId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual KhachHang KhachHang { get; set; } = null!;
}
