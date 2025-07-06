
namespace TraSuaApp.Shared.Dtos;

public class NhomSanPhamDto
{
    public int STT { get; set; }
    public string? TenNormalized { get; set; }


    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public int? IdOld { get; set; }
}