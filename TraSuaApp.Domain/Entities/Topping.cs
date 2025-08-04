using System.ComponentModel.DataAnnotations.Schema;
using TraSuaApp.Infrastructure.Data;

namespace TraSuaApp.Domain.Entities;

public partial class Topping : EntityBase
{
    public string Ten { get; set; } = null!;

    public decimal Gia { get; set; }

    public bool NgungBan { get; set; }

    [NotMapped]
    public int OldId { get; set; }

    public virtual ICollection<ChiTietHoaDonTopping> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonTopping>();

    public virtual ICollection<ToppingNhomSanPhams> ToppingNhomSanPhams { get; set; } = new List<ToppingNhomSanPhams>();
    public virtual ICollection<NhomSanPham> NhomSanPhams { get; set; } = new List<NhomSanPham>();
}





