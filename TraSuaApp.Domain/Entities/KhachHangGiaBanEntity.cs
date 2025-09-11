namespace TraSuaApp.Domain.Entities;

public partial class KhachHangGiaBan
{
    public Guid Id { get; set; }

    public Guid KhachHangId { get; set; }
    public Guid SanPhamBienTheId { get; set; }
    public decimal GiaBan { get; set; }

    // Navigation
    public virtual KhachHang KhachHang { get; set; } = null!;
    public virtual SanPhamBienThe SanPhamBienThe { get; set; } = null!;


    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

}
