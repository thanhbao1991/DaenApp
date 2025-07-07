using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class CustomerPoint
{
    public Guid Id { get; set; }

    public Guid IdKhachHang { get; set; }

    public int TongDiem { get; set; }

    public Guid KhachHangId { get; set; }

    public virtual KhachHang KhachHang { get; set; } = null!;
}
