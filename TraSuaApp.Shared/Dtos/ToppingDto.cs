namespace TraSuaApp.Shared.Dtos;

public class ToppingDto
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;

    public int STT { get; set; }
    public string? TenNormalized { get; set; }
    public List<Guid> IdNhomSanPham { get; set; } = new();
}