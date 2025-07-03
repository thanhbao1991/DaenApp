namespace TraSuaApp.Domain.Entities;

public class ChiTietTuyChinhMon
{
    public Guid Id { get; set; }
    public Guid IdTuyChinhMon { get; set; }
    public string GiaTri { get; set; } = string.Empty; // Ví dụ: "Ít", "Vừa", "Nhiều"

    public TuyChinhMon TuyChinhMon { get; set; }
}