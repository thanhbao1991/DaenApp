namespace TraSuaApp.Domain.Entities;

public partial class NhomSanPham
{

    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Guid Id { get; set; }
    public string Ten { get; set; } = null!;
    public int STT { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();

    public virtual ICollection<Topping> IdToppings { get; set; } = new List<Topping>();
}
