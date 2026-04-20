namespace TraSuaApp.Infrastructure.Dtos;

public class SanPhamBienTheDto : DtoBase
{


    public string? DinhLuong { get; set; }

    public int Stt { get; set; }

    public override string ApiRoute => "SanPhamBienThe";

    public Guid SanPhamId { get; set; }
    public string TenBienThe { get; set; } = string.Empty;
    public decimal GiaBan { get; set; }
    public bool MacDinh { get; set; }

}
