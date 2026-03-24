namespace TraSuaApp.Shared.Dtos
{
    public class ChiTieuItemDto
    {
        public string Ten { get; set; } = "";
        public decimal SoTien { get; set; }
    }

    public class ThongKeChiTieuDto
    {
        public decimal TongChiTieu { get; set; }

        public decimal ChiTieuNgay { get; set; }
        public List<ChiTieuItemDto> DanhSachChiTieuNgay { get; set; } = new();

        public decimal ChiTieuThang { get; set; }
        public List<ChiTieuItemDto> DanhSachChiTieuThang { get; set; } = new();
    }
}

namespace TraSuaApp.Shared.Dtos
{
    public class CongNoItemDto
    {
        public string TenKhachHang { get; set; } = "";
        public decimal SoTienNo { get; set; }
    }

    public class ThongKeCongNoDto
    {
        public decimal TongCongNoNgay { get; set; }

        public List<CongNoItemDto> DanhSachCongNoNgay { get; set; } = new();
    }
}

namespace TraSuaApp.Shared.Dtos
{
    public class ThanhToanItemDto
    {
        public string Ten { get; set; } = "";
        public decimal SoTien { get; set; }
    }

    public class ThongKeThanhToanDto
    {
        // Tổng tiền mặt (4 loại cộng lại)
        public decimal TongTienMat { get; set; }

        // Tổng chuyển khoản
        public decimal TongChuyenKhoan { get; set; }

        // Danh sách chi tiết tiền mặt
        public List<ThanhToanItemDto> DanhSachTienMat { get; set; } = new();
    }


}

namespace TraSuaApp.Shared.Dtos
{
    public class DoanhThuTheoLoaiItemDto
    {
        public string Ten { get; set; } = "";
        public decimal DoanhThu { get; set; }
    }

    public class ThongKeDoanhThuNgayDto
    {
        public decimal TongDoanhThu { get; set; }
        public List<DoanhThuTheoLoaiItemDto> DanhSach { get; set; } = new();
    }
}

namespace TraSuaApp.Shared.Dtos
{
    public class KhachTraNoItemDto
    {
        public string TenKhachHang { get; set; } = "";
        public decimal SoTien { get; set; }
    }

    public class ThongKeTraNoNgayDto
    {
        public decimal TongTraNoTaiQuan { get; set; }
        public decimal TongTraNoShipper { get; set; }

        public List<KhachTraNoItemDto> TraNoTaiQuan { get; set; } = new();
        public List<KhachTraNoItemDto> TraNoShipper { get; set; } = new();
    }


}


namespace TraSuaApp.Shared.Dtos
{

    public class ThongKeDonChuaThanhToanDto
    {
        public decimal TongChuaThanhToan { get; set; }

        public List<DonChuaThanhToanItemDto> DanhSach { get; set; } = new();
    }

    public class DonChuaThanhToanItemDto
    {
        public string TenKhachHang { get; set; } = "";
        public decimal SoTien { get; set; }
    }
}