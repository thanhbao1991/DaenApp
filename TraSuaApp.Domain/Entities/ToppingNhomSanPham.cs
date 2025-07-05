namespace TraSuaApp.Domain.Entities
{
    public class ToppingNhomSanPham
    {
        public Guid IdTopping { get; set; }
        public Guid IdNhomSanPham { get; set; }

        public Topping Topping { get; set; } = null!;
        public NhomSanPham NhomSanPham { get; set; } = null!;
    }
}