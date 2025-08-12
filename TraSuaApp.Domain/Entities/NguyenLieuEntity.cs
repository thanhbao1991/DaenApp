namespace TraSuaApp.Domain.Entities;

public partial class NguyenLieu
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;
    public int OldId { get; set; }

    public string? DonViTinh { get; set; }

    public decimal? TonKho { get; set; }

    public decimal GiaNhap { get; set; }

    public bool DangSuDung { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public virtual ICollection<ChiTietHoaDonNhapEntity> ChiTietHoaDonNhaps { get; set; } = new List<ChiTietHoaDonNhapEntity>();

    public virtual ICollection<LichSuNhapXuatKho> LichSuNhapXuatKhos { get; set; } = new List<LichSuNhapXuatKho>();

    public virtual ICollection<SuDungNguyenLieu> SuDungNguyenLieus { get; set; } = new List<SuDungNguyenLieu>();
}
