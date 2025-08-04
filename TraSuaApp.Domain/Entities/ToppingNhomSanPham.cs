using TraSuaApp.Domain.Entities;

namespace TraSuaApp.Infrastructure.Data;

public partial class ToppingNhomSanPhams : EntityBase
{
    public Guid ToppingId { get; set; }

    public Guid NhomSanPhamId { get; set; }


    public virtual NhomSanPham NhomSanPham { get; set; } = null!;

    public virtual Topping Topping { get; set; } = null!;
}

