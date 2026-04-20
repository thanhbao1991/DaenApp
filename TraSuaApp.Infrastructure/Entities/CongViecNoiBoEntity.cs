namespace TraSuaApp.Infrastructure.Entities;

public partial class CongViecNoiBo
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public bool DaHoanThanh { get; set; }
    public DateTime? NgayGio { get; set; }

    // ✅ Trường mới
    public DateTime? NgayCanhBao { get; set; }
    public int? XNgayCanhBao { get; set; }

    
    
    
    public DateTime? LastModified { get; set; }
}