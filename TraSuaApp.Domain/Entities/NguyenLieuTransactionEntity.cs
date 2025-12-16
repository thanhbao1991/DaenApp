namespace TraSuaApp.Domain.Entities;

public enum LoaiGiaoDichNguyenLieu
{
    Nhap = 1,      // Mua hàng, nhập kho (dương)
    XuatBan = 2,   // Xuất do bán ly cho khách (âm)
    XuatKhac = 3,  // Đổ bỏ, pha sai... (âm)
    DieuChinh = 4  // Kiểm kê, chỉnh lệch (+/-)
}

public partial class NguyenLieuTransaction
{
    public Guid Id { get; set; }

    /// <summary>
    /// FK -> NguyenLieuBanHang.Id (đơn vị bán nhỏ nhất)
    /// </summary>
    public Guid NguyenLieuId { get; set; }

    public DateTime NgayGio { get; set; }

    public LoaiGiaoDichNguyenLieu Loai { get; set; }

    /// <summary>
    /// Nhập: dương, Xuất: âm, Điều chỉnh: +/-.
    /// </summary>
    public decimal SoLuong { get; set; }

    public decimal? DonGia { get; set; }
    public string? GhiChu { get; set; }

    public Guid? ChiTieuHangNgayId { get; set; }
    public Guid? HoaDonId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsDeleted { get; set; }

    // Nav (nếu bạn có cấu hình)
    public virtual NguyenLieuBanHang? NguyenLieu { get; set; }
}
