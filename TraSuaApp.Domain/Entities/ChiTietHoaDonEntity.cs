namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonEntity
{
    public Guid Id { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public decimal ThanhTien { get; set; }

    public Guid HoaDonId { get; set; }

    public Guid SanPhamBienTheId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }
    public int Stt
    {
        get; set;
    }
    public DateTime? LastModified { get; set; }

    public string TenBienThe { get; set; } = null!;

    public string TenSanPham { get; set; } = null!;

    public string ToppingText { get; set; } = null!;

    public string? NoteText { get; set; }

    public int OldId { get; set; }

    public virtual HoaDon HoaDon { get; set; } = null!;

    public virtual SanPhamBienThe SanPhamBienThe { get; set; } = null!;
}
