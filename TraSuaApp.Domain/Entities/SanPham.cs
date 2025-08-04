using System.ComponentModel.DataAnnotations.Schema;

namespace TraSuaApp.Domain.Entities;

public partial class SanPham : EntityBase
{
    public string Ten { get; set; } = null!;

    public string? DinhLuong { get; set; }

    public string? VietTat { get; set; }

    public int? DaBan { get; set; }

    public bool NgungBan { get; set; }

    public bool TichDiem { get; set; }

    [NotMapped]
    public int OldId { get; set; }
    public Guid NhomSanPhamId { get; set; }

    public virtual NhomSanPham? NhomSanPham { get; set; }
    public virtual ICollection<SanPhamBienThe> SanPhamBienThes { get; set; } = new List<SanPhamBienThe>();
}





