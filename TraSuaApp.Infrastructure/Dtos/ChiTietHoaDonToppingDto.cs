namespace TraSuaApp.Infrastructure.Dtos;

public class ChiTietHoaDonToppingDto : DtoBase
{
                         public int Stt { get; set; }     public string Ten { get; set; } = null!;   public override string ApiRoute => "ChiTietHoaDonTopping";
    public Guid ChiTietHoaDonId { get; set; }
    public Guid HoaDonId { get; set; }
    public Guid ToppingId { get; set; }
    public int SoLuong { get; set; }
    public decimal Gia { get; set; }
}
