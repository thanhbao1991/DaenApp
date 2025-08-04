namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonThanhToan : EntityBase
{

    public Guid HoaDonId { get; set; }
    public Guid PhuongThucThanhToanId { get; set; }

    public decimal SoTien { get; set; }

    public DateTime Ngay { get; set; }

    public DateTime NgayGio { get; set; }

    public virtual HoaDon? HoaDon { get; set; }

    public virtual PhuongThucThanhToan? PhuongThucThanhToan { get; set; }

}




