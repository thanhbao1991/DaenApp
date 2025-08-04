namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonNhap : EntityBase
{

    public Guid HoaDonIdNhap { get; set; }

    public Guid NguyenLieuId { get; set; }

    public decimal SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public Guid HoaDonNhapId { get; set; }


    public virtual HoaDonNhap? HoaDonNhap { get; set; }

    public virtual NguyenLieu? NguyenLieu { get; set; }
}




