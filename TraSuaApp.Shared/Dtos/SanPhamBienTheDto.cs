namespace TraSuaApp.Shared.Dtos;

public class SanPhamBienTheDto
{
    public Guid Id { get; set; }
    public Guid IdSanPham { get; set; }
    public string TenBienThe { get; set; } = string.Empty;
    public decimal GiaBan { get; set; }
    public bool MacDinh { get; set; }

}