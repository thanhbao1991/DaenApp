namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonNo : EntityBase
{
    public Guid? KhachHangId { get; set; }

    public Guid HoaDonId { get; set; }

    public decimal SoTienNo { get; set; }

    public decimal SoTienDaTra { get; set; }

    public DateTime Ngay { get; set; }

    public DateTime NgayGio { get; set; }

    public virtual HoaDon? HoaDon { get; set; }
}




