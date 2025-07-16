namespace TraSuaApp.Domain.Entities;

public partial class Topping
{
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public decimal Gia { get; set; }

    public bool NgungBan { get; set; }

    public virtual ICollection<ChiTietHoaDonTopping> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonTopping>();

    public virtual ICollection<NhomSanPham> IdNhomSanPhams { get; set; } = new List<NhomSanPham>();
}
