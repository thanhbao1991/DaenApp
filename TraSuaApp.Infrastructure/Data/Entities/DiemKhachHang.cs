using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class DiemKhachHang
{
    public Guid Id { get; set; }

    public int TongDiem { get; set; }

    public Guid KhachHangId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual KhachHang KhachHang { get; set; } = null!;
}
