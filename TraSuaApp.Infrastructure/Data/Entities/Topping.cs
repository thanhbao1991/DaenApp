using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class Topping
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public decimal Gia { get; set; }

    public bool NgungBan { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public int OldId { get; set; }

    public virtual ICollection<ChiTietHoaDonTopping> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonTopping>();

    public virtual ICollection<NhomSanPham> NhomSanPhams { get; set; } = new List<NhomSanPham>();
}
