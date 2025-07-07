using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class Topping
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public decimal Gia { get; set; }

    public bool NgungBan { get; set; }

    public virtual ICollection<ChiTietHoaDonTopping> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonTopping>();

    public virtual ICollection<NhomSanPham> IdNhomSanPhams { get; set; } = new List<NhomSanPham>();
}
