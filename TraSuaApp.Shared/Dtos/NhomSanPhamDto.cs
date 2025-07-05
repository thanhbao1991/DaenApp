
namespace TraSuaApp.Shared.Dtos;

public class NhomSanPhamDto
{
    public Guid? Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public int? IdOld { get; set; }
}