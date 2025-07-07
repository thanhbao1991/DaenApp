using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class KhachHangAddress
{
    public Guid Id { get; set; }

    public Guid IdKhachHang { get; set; }

    public string DiaChi { get; set; } = null!;

    public bool IsDefault { get; set; }

    public Guid KhachHangId { get; set; }

    public virtual KhachHang KhachHang { get; set; } = null!;
}
