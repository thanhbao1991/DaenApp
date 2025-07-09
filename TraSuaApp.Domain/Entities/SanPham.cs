namespace TraSuaApp.Domain.Entities;

public partial class SanPham
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public string? DinhLuong { get; set; }

    public string? VietTat { get; set; }

    public int? DaBan { get; set; }


    public bool NgungBan { get; set; }

    public bool TichDiem { get; set; }

    public int IdOld { get; set; }

    public Guid? IdNhomSanPham { get; set; }

    public virtual NhomSanPham? IdNhomSanPhamNavigation { get; set; }
    public virtual ICollection<SanPhamBienThe> SanPhamBienThes { get; set; } = new List<SanPhamBienThe>();
}
