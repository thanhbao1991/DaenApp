namespace TraSuaApp.Domain.Entities;

public partial class HoaDonNhap : EntityBase
{

    public DateTime NgayNhap { get; set; }


    public string? GhiChu { get; set; }


    public virtual ICollection<ChiTietHoaDonNhap> ChiTietHoaDonNhaps { get; set; } = new List<ChiTietHoaDonNhap>();

}





