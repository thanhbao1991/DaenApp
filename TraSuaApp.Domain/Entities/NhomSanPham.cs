using TraSuaApp.Infrastructure.Data;

namespace TraSuaApp.Domain.Entities;

public partial class NhomSanPham : EntityBase
{

    public string Ten { get; set; } = null!;


    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();

    public virtual ICollection<ToppingNhomSanPhams> ToppingNhomSanPhams { get; set; } = new List<ToppingNhomSanPhams>();

    public virtual ICollection<Topping> Toppings { get; set; } = new List<Topping>();

}





