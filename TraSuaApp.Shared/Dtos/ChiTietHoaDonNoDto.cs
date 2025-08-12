namespace TraSuaApp.Shared.Dtos;

public class ChiTietHoaDonNoDto : DtoBase
{
    public override string ApiRoute => "ChiTietHoaDonNo";
    public decimal SoTienNo { get; set; }
    public decimal SoTienDaTra { get; set; }
    public decimal SoTienConNo => SoTienNo - SoTienDaTra;
    public DateTime NgayGio { get; set; }
    public DateTime Ngay { get; set; }

    public Guid HoaDonId { get; set; }
    public Guid? KhachHangId { get; set; }


}