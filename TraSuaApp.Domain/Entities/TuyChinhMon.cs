namespace TraSuaApp.Domain.Entities;

public partial class TuyChinhMon : EntityBase
{

    public string Ten { get; set; } = null!;

    public bool ChoPhepChonNhieuGiaTri { get; set; }

    public virtual ICollection<ChiTietTuyChinhMon> ChiTietTuyChinhMons { get; set; } = new List<ChiTietTuyChinhMon>();
}





