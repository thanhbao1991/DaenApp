namespace TraSuaApp.Domain.Entities;

public class NhomSanPham
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public int? IdOld { get; set; }


    public ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
}