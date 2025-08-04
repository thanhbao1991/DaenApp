namespace TraSuaApp.Shared.Dtos;

public class SanPhamBienTheDto
{
    public Guid Id { get; set; }
    public Guid SanPhamId { get; set; }
    public string TenBienThe { get; set; } = string.Empty;
    public decimal GiaBan { get; set; }
    public bool MacDinh { get; set; }

}
