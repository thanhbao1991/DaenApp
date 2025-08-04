namespace TraSuaApp.Domain.Entities;

public partial class PhuongThucThanhToan : EntityBase
{

    public required string Ten { get; set; }
    public bool DangSuDung { get; set; }

    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();
}



