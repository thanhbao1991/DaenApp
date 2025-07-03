namespace TraSuaApp.Domain.Entities;

public class LichSuChinhSua
{
    public Guid Id { get; set; }
    public DateTime ThoiGian { get; set; }
    public Guid IdTaiKhoan { get; set; }
    public string LoaiThaoTac { get; set; } = string.Empty;
    public string? GhiChu { get; set; }

    public TaiKhoan TaiKhoan { get; set; }
}