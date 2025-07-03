namespace TraSuaApp.Domain.Entities;

public class SanPham
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public string? VietTat { get; set; }
    public int? DaBan { get; set; }
    public Guid? IdNhomSanPham { get; set; }

    public ICollection<SanPhamBienThe> BienThe { get; set; }
}