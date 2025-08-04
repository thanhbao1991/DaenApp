namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonTopping : EntityBase
{
    public Guid ChiTietHoaDonId { get; set; }

    public Guid HoaDonId { get; set; }

    public Guid ToppingId { get; set; }

    public int SoLuong { get; set; }

    public decimal Gia { get; set; }

    //0 public string ToppingText { get; set; }


    public virtual HoaDon? HoaDon { get; set; }

    public virtual Topping? Topping { get; set; }
    public required string TenTopping { get; set; }
}




