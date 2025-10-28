namespace TraSuaApp.Domain.Entities;

public partial class SanPham
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public string? DinhLuong { get; set; }

    public string? VietTat { get; set; }
    public string? TenKhongVietTat { get; set; }
    public string TimKiem { get; set; }

    public int ThuTu { get; set; }

    public Guid? NhomSanPhamId { get; set; }

    public bool NgungBan { get; set; }

    public bool TichDiem { get; set; }

    public int OldId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual NhomSanPham? NhomSanPham { get; set; }

    public virtual ICollection<SanPhamBienThe> SanPhamBienThes { get; set; } = new List<SanPhamBienThe>();
}
