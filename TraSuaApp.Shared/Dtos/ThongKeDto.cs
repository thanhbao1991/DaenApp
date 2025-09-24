namespace TraSuaApp.Shared.Dtos
{
    public class LabelValueDto
    {
        public string Ten { get; set; } = "";
        public decimal GiaTri { get; set; }
    }

    public class ThongKeNgayDto
    {
        public DateTime Ngay { get; set; }

        // Tổng hợp cột trái (y nguyên thứ tự giao diện cũ)
        public decimal DoanhThu { get; set; }
        public decimal DaThu { get; set; }
        public decimal DaThu_TienMat { get; set; }
        public decimal DaThu_Banking { get; set; }
        public decimal DaThu_Khanh { get; set; } // Tiền mặt có GhiChu="Shipper"
        public decimal ChuaThu { get; set; }
        public decimal ChiTieu { get; set; }
        public decimal CongNo { get; set; }
        public decimal MangVe { get; set; }
        public decimal TraNoTien { get; set; }
        public decimal TraNoKhanh { get; set; } // Trả nợ có GhiChu="Shipper"
        public decimal TraNoBank { get; set; }

        public decimal TongTraNo => TraNoTien + TraNoBank + TraNoKhanh;
        public int TongSoDon { get; set; }
        public int TongSoLy { get; set; }

        // Card chi tiết (giống giao diện cũ)
        public List<LabelValueDto> DoanhThuChiTiet { get; set; } = new();  // Ship / Tại chỗ / App
        public List<LabelValueDto> ChiTieuChiTiet { get; set; } = new();   // Bút, Bát, ...
        public List<LabelValueDto> CongNoChiTiet { get; set; } = new();    // Khách + số nợ
        public List<LabelValueDto> TraNoTienChiTiet { get; set; } = new(); // Khách + số trả
        public List<LabelValueDto> TraNoBankChiTiet { get; set; } = new(); // Khách + số trả
        public List<LabelValueDto> DaThuChiTiet { get; set; } = new();     // Tiền mặt / Chuyển khoản (dùng cho card “Đã thu”)
        public List<LabelValueDto> ChuaThuChiTiet { get; set; } = new(); // 🟟 thêm

        // Top bán chạy
        public List<TopSanPhamDto> TopSanPhams { get; set; } = new();
    }

    public class TopSanPhamDto
    {
        public int Stt { get; set; }
        public string TenSanPham { get; set; } = "";
        public decimal SoLuong { get; set; }
        public decimal DoanhThu { get; set; }
    }
}