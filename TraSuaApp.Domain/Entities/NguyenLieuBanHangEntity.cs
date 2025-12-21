using System.ComponentModel.DataAnnotations.Schema;

namespace TraSuaApp.Domain.Entities;

public partial class NguyenLieuBanHang
{
    public Guid Id { get; set; }

    public string Ten { get; set; } = null!;

    public bool DangSuDung { get; set; }

    // Đơn vị bán nhỏ nhất: Lon, Gram, Ml...
    public string? DonViTinh { get; set; }

    // Tồn kho theo đơn vị bán (lon/ml/gram...)
    [Column(TypeName = "decimal(18,2)")]
    public decimal TonKho { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    // Giữ lại nav cũ nếu đang dùng ở nơi khác
    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();

    // 🟟 Các dòng NguyenLieu nhập hàng map về đây
    public virtual ICollection<NguyenLieu> NguyenLieus { get; set; } = new List<NguyenLieu>();

    // 🟟 Công thức sử dụng nguyên liệu bán (SuDungNguyenLieu)
    public virtual ICollection<SuDungNguyenLieu> SuDungNguyenLieus { get; set; } = new List<SuDungNguyenLieu>();
}