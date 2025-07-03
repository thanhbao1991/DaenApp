namespace TraSuaApp.Domain.Entities;

public class NoHoaDon
{
    public Guid Id { get; set; }
    public Guid IdHoaDon { get; set; }
    public decimal SoTienNo { get; set; }
    public decimal SoTienDaTra { get; set; }
    public DateTime NgayGhiNhan { get; set; }

    public HoaDon HoaDon { get; set; }
}