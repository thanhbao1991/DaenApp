using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class CongViecNoiBo
{
    public Guid Id { get; set; }

    public DateTime Ngay { get; set; }

    public string NoiDung { get; set; } = null!;

    public bool DaHoanThanh { get; set; }

    public Guid IdNguoiTao { get; set; }

    public DateTime ThoiGianTao { get; set; }

    public DateTime? ThoiGianHoanThanh { get; set; }

    public Guid NguoiTaoId { get; set; }

    public virtual TaiKhoan NguoiTao { get; set; } = null!;
}
