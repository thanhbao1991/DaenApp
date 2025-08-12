using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class TaiKhoan
{
    public Guid Id { get; set; }

    public string TenDangNhap { get; set; } = null!;

    public string MatKhau { get; set; } = null!;

    public string? VaiTro { get; set; }

    public bool IsActive { get; set; }

    public string? TenHienThi { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }
}
