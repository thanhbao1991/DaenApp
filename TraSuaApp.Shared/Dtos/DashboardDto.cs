using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public class DashboardDto
{
    public List<ChiTietHoaDonDto> History { get; set; } = new();
}

namespace TraSuaApp.Shared.Dtos
{
    public class SanPhamXepHangDto
    {
        public string TenSanPham { get; set; } = "";
        public string TenTimKiem => TenSanPham.MyNormalizeText();
        public int TongSoLuong { get; set; }
        public decimal TongDoanhThu { get; set; }
        public int Stt { get; set; }  // để hiển thị trong bảng
    }

    public class KhachHangXepHangDto
    {
        public Guid KhachHangId { get; set; }
        public string TenKhachHang { get; set; } = "";
        public string? SoDienThoai { get; set; }

        // Cho bộ lọc không dấu
        public string TenTimKiem => ($"{TenKhachHang} {SoDienThoai}").MyNormalizeText();

        public int TongSoDon { get; set; } // COUNT(h.Id)
        public decimal TongDoanhThu { get; set; } // SUM(h.ThanhTien)
        public DateTime? LanCuoiMua { get; set; } // MAX(h.NgayGio)

        public int Stt { get; set; }
    }
}