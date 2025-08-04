namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonTopping
{
    public Guid Id { get; set; }

    public int SoLuong { get; set; }

    public decimal Gia { get; set; }

    public Guid HoaDonId { get; set; }

    public Guid ToppingId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public Guid ChiTietHoaDonId { get; set; }

    public string TenTopping { get; set; } = null!;

    public virtual HoaDon HoaDon { get; set; } = null!;

    public virtual Topping Topping { get; set; } = null!;
}
