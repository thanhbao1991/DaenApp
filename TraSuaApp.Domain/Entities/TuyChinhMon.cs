namespace TraSuaApp.Domain.Entities;

public class TuyChinhMon
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty; // Ví dụ: Đường, Đá, Trà
    public bool ChoPhepChonNhieuGiaTri { get; set; }

    public ICollection<ChiTietTuyChinhMon> ChiTietTuyChinhMons { get; set; }
}