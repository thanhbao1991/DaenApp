namespace TraSuaApp.Domain.Entities;

public class CustomerPoint
{
    public Guid Id { get; set; }
    public Guid IdKhachHang { get; set; }
    public int TongDiem { get; set; }

    public KhachHang KhachHang { get; set; }
}