namespace TraSuaApp.Shared.Dtos;

public class DoanhThuHoaDonDto
{
    public Guid Id { get; set; }
    public Guid? IdKhachHang { get; set; }
    public string? TenKhachHangText { get; set; }
    public string? DiaChi { get; set; }
    public string? ThongTinHoaDon { get; set; }
    public string? DiaChiShip { get; set; }
    public string? PhanLoai { get; set; }

    public DateTime NgayHoaDon { get; set; }
    public DateTime? NgayShip { get; set; }
    public DateTime? NgayNo { get; set; }
    public DateTime? NgayTra { get; set; }

    public bool DaThanhToan { get; set; }
    public bool BaoDon { get; set; }

    public decimal TongTien { get; set; }
    public decimal DaThu { get; set; }
    public decimal ConLai { get; set; }
    public decimal TienBank { get; set; }
    public decimal TienNo { get; set; }
    public decimal TienMat { get; set; }
}

public class DoanhThuNgayDto
{
    public DateTime Ngay { get; set; }
    public decimal TongDoanhThu { get; set; }
    public decimal TongDaThu { get; set; }
    public decimal TongConLai { get; set; }
    public decimal TongChiTieu { get; set; }
    public decimal TongSoDon { get; set; }
    public decimal TongChuyenKhoan { get; set; }
    public decimal TongTienMat { get; set; }
    public decimal TongTienNo { get; set; }
    public decimal TongCongNo { get; set; }
    public string ViecChuaLam { get; set; }
    public decimal MuaVe { get; set; }
    public decimal TaiCho { get; set; }
    public decimal DiShip { get; set; }
    public decimal AppShipping { get; set; }

    public List<DoanhThuHoaDonDto> HoaDons { get; set; } = new();
}

public class DoanhThuThangItemDto
{
    public DateTime Ngay { get; set; }
    public int SoDon { get; set; }
    public decimal TongTien { get; set; }
    public decimal ChiTieu { get; set; }
    public decimal TienBank { get; set; }
    public decimal TienNo { get; set; }
    public decimal TongTienMat { get; set; }
    public decimal TaiCho { get; set; }
    public decimal MuaVe { get; set; }
    public decimal DiShip { get; set; }
    public decimal AppShipping { get; set; }
    public decimal ThuongNha { get; set; }
    public decimal ThuongKhanh { get; set; }
}

// THEO GIỜ trong THÁNG
public class DoanhThuHourBucketDto
{
    public int Hour { get; set; }      // 0..23
    public int SoDon { get; set; }     // giữ để tương thích
    public decimal DoanhThu { get; set; } // mới: tổng ThanhTien theo giờ
}

public class DoanhThuChiTietHoaDonDto
{
    public Guid Id { get; set; }
    public string TenSanPham { get; set; } = "";
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
    public string? GhiChu { get; set; }
}