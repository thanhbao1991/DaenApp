using System.ComponentModel.DataAnnotations.Schema;
using TraSuaApp.Domain.Entities;

public partial class ChiTieuHangNgay
{
    public Guid Id { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SoLuong { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal DonGia { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal ThanhTien { get; set; }
    public Boolean BillThang { get; set; }
    public string Ten { get; set; } = null!;

    public DateTime Ngay { get; set; }
    public DateTime NgayGio { get; set; }
    public Guid NguyenLieuId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? LastModified { get; set; }

    public virtual NguyenLieu NguyenLieu { get; set; } = null!;
}