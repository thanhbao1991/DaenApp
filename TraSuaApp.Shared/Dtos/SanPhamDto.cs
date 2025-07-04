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
    public int IdOld { get; set; }
    public string? TenNormalized { get; set; }

    public List<SanPhamBienTheDto> BienThe { get; set; } = new();
}

public class SanPhamBienTheDto
{
    public Guid Id { get; set; }
    public Guid IdSanPham { get; set; }
    public string TenBienThe { get; set; } = string.Empty;
    public decimal GiaBan { get; set; }
}