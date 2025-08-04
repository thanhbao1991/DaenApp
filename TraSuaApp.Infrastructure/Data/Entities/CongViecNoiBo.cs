using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class CongViecNoiBo
{
    public Guid Id { get; set; }

    public DateTime Ngay { get; set; }

    public string NoiDung { get; set; } = null!;

    public bool DaHoanThanh { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime? ThoiGianHoanThanh { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }
}
