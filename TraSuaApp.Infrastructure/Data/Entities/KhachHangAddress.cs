using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class KhachHangAddress
{
    public Guid Id { get; set; }

    public string DiaChi { get; set; } = null!;

    public bool IsDefault { get; set; }

    public Guid KhachHangId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual KhachHang KhachHang { get; set; } = null!;
}
