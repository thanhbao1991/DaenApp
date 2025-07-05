namespace TraSuaApp.Domain.Entities;

public class SanPhamBienThe
{
    public Guid Id { get; set; }
    public Guid IdSanPham { get; set; }
    public string TenBienThe { get; set; } = string.Empty; // Ví dụ: Size M, Size L
    public decimal GiaBan { get; set; }
    public bool MacDinh { get; set; }

    public SanPham SanPham { get; set; }
}