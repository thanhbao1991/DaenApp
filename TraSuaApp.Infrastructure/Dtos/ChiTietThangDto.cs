namespace TraSuaApp.Infrastructure.Dtos
{
    public class ChiTietThangDto
    {
        public DateTime NgayGio { get; set; }

        public string TenKhachHang { get; set; } = "";
        public string TenSanPham { get; set; } = "";
        public string TenBienThe { get; set; } = "";

        public int SoLuong { get; set; }
    }
}