using System.ComponentModel.DataAnnotations.Schema;

namespace TraSuaApp.Domain.Entities;

public partial class NguyenLieu
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;
    public int OldId { get; set; }

    public string? DonViTinh { get; set; }


    public decimal GiaNhap { get; set; }

    public bool DangSuDung { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    // 🟟 Map sang NguyenLieuBanHang (đơn vị bán nhỏ nhất, vd: lon, ml, gram...)
    public Guid? NguyenLieuBanHangId { get; set; }

    /// <summary>
    /// 1 đơn vị nhập (thùng / lốc / kg...) = bao nhiêu đơn vị bán
    /// VD: 1 thùng Bò húc = 24 lon -> HeSoQuyDoiBanHang = 24
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal? HeSoQuyDoiBanHang { get; set; }

    public virtual NguyenLieuBanHang? NguyenLieuBanHang { get; set; }

    public virtual ICollection<ChiTietHoaDonNhapEntity> ChiTietHoaDonNhaps { get; set; } = new List<ChiTietHoaDonNhapEntity>();

    public virtual ICollection<SuDungNguyenLieu> SuDungNguyenLieus { get; set; } = new List<SuDungNguyenLieu>();
}