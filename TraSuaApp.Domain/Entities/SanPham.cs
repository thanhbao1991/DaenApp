using System.ComponentModel.DataAnnotations.Schema;

namespace TraSuaApp.Domain.Entities;

public class SanPham
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public string? VietTat { get; set; }
    public int? DaBan { get; set; }
    public bool TichDiem { get; set; }
    public bool NgungBan { get; set; }
    public string? DinhLuong { get; set; }
    public int IdOld { get; set; }
    public Guid? IdNhomSanPham { get; set; }
    [ForeignKey(nameof(IdNhomSanPham))]
    public NhomSanPham? NhomSanPham { get; set; }
    public ICollection<SanPhamBienThe> BienThe { get; set; }
}