using System.ComponentModel.DataAnnotations.Schema;

namespace TraSuaApp.Domain.Entities;

public partial class CongViecNoiBo : EntityBase
{

    public DateTime Ngay { get; set; }

    public string NoiDung { get; set; } = null!;

    public bool DaHoanThanh { get; set; }

    [NotMapped]
    public Guid NguoiTaoId { get; set; }
    public DateTime ThoiGianTao { get; set; }

    public DateTime? ThoiGianHoanThanh { get; set; }


}




