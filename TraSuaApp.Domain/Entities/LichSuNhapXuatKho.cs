namespace TraSuaApp.Domain.Entities;

public partial class LichSuNhapXuatKho : EntityBase
{

    public DateTime ThoiGian { get; set; }


    public decimal SoLuong { get; set; }

    public string Loai { get; set; } = null!;

    public string? GhiChu { get; set; }

    public Guid NguyenLieuId { get; set; }

    public virtual NguyenLieu? NguyenLieu { get; set; }
}




