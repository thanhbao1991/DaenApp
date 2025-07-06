namespace TraSuaApp.Domain.Entities;

public class Topping
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = string.Empty;

    public decimal Gia { get; set; }

    public bool NgungBan { get; set; }

    public List<ToppingNhomSanPham> DanhSachNhomSanPham { get; set; } = new();
}
