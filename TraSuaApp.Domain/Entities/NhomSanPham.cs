using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class NhomSanPham
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public int? IdOld { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();

    public virtual ICollection<Topping> IdToppings { get; set; } = new List<Topping>();
}
