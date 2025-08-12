using System.ComponentModel.DataAnnotations.Schema;

namespace TraSuaApp.Domain.Entities;

[Table("ChiTietHoaDonPoints")]
public class ChiTietHoaDonPoint
{
    public Guid HoaDonId { get; set; }

    public Guid Id { get; set; }

    public DateTime Ngay { get; set; }

    public DateTime NgayGio { get; set; }

    public int DiemThayDoi { get; set; }

    public required string GhiChu { get; set; }

    public Guid KhachHangId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual KhachHang KhachHang { get; set; } = null!;
}
