namespace TraSuaApp.Shared.Dtos;

public class SanPhamDto
{
    public int STT { get; set; }

    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string? DinhLuong { get; set; }
    public string? VietTat { get; set; }
    public int? DaBan { get; set; }
    public bool TichDiem { get; set; }
    public bool NgungBan { get; set; }
    public Guid? IdNhomSanPham { get; set; }
    public string? TenNhomSanPham { get; set; }
    public int IdOld { get; set; }


    public List<SanPhamBienTheDto> BienThe { get; set; } = new();
}
