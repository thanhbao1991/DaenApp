namespace TraSuaApp.Shared.Dtos;

public class SanPhamDto
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? VietTat { get; set; }
    public int? DaBan { get; set; }
    public Guid? IdNhomSanPham { get; set; }

    public List<SanPhamBienTheDto> BienThe { get; set; } = new();
}

public class SanPhamBienTheDto
{
    public Guid Id { get; set; }
    public Guid IdSanPham { get; set; }
    public string TenBienThe { get; set; } = string.Empty;
    public decimal GiaBan { get; set; }
}