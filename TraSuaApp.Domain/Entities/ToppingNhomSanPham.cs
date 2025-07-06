using System.ComponentModel.DataAnnotations.Schema;

namespace TraSuaApp.Domain.Entities
{
    public class ToppingNhomSanPham
    {
        public Guid IdTopping { get; set; }
        public Guid IdNhomSanPham { get; set; }

        [ForeignKey(nameof(IdTopping))]
        public Topping Topping { get; set; } = null!;

        [ForeignKey(nameof(IdNhomSanPham))]
        public NhomSanPham NhomSanPham { get; set; } = null!;
    }
}