namespace TraSuaApp.Domain.Entities;

public class CustomerPointLog
{
    public Guid Id { get; set; }
    public Guid IdKhachHang { get; set; }
    public DateTime ThoiGian { get; set; }
    public int DiemThayDoi { get; set; }
    public string? GhiChu { get; set; }

    public KhachHang KhachHang { get; set; }
}