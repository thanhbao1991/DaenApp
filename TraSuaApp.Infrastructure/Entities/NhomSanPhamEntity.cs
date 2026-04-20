namespace TraSuaApp.Infrastructure.Entities;

public partial class NhomSanPham
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public DateTime? LastModified { get; set; }






    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();

    public virtual ICollection<Topping> Toppings { get; set; } = new List<Topping>();
}
