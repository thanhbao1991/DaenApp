namespace TraSuaApp.Domain.Entities;

public partial class CongViecNoiBo
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public bool DaHoanThanh { get; set; }
    public DateTime? NgayGio { get; set; }   // ✅ Thêm dòng này


    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }


}
