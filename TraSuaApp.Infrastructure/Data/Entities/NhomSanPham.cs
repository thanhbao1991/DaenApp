using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class NhomSanPham
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public DateTime? LastModified { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();

    public virtual ICollection<Topping> Toppings { get; set; } = new List<Topping>();
}
