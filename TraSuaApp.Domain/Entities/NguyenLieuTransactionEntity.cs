namespace TraSuaApp.Domain.Entities;

public enum LoaiGiaoDichNguyenLieu
{
    Nhap = 1,      // Mua hàng, nhập kho
    XuatBan = 2,   // Xuất do bán ly cho khách
    XuatKhac = 3,  // Đổ bỏ, pha sai...
    DieuChinh = 4  // Kiểm kê, chỉnh lệch
}

public partial class NguyenLieuTransaction
{
    public Guid Id { get; set; }

    // ✅ FK -> NguyenLieuBanHang.Id (đơn vị bán)
    public Guid NguyenLieuId { get; set; }

    public DateTime NgayGio { get; set; }

    public LoaiGiaoDichNguyenLieu Loai { get; set; }

    /// <summary>
    /// Nhập: +, Xuất: -, Điều chỉnh: +/- theo delta
    /// </summary>
    public decimal SoLuong { get; set; }

    public decimal? DonGia { get; set; }
    public string? GhiChu { get; set; }

    public Guid? ChiTieuHangNgayId { get; set; }
    public Guid? HoaDonId { get; set; }

    public DateTime CreatedAt { get; set; }

    // ✅ nullable cho soft-delete
    public DateTime? DeletedAt { get; set; }

    public DateTime LastModified { get; set; }
    public bool IsDeleted { get; set; }
}
