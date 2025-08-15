namespace TraSuaApp.Shared.Dtos
{

    public class DashboardTopSanPhamDto
    {
        public int Stt { get; set; }            // số thứ tự
        public DateTime Ngay { get; set; }      // ngày bán
        public string TenSanPham { get; set; }
        public decimal SoLuong { get; set; }
        public decimal DoanhThu { get; set; }
        public string TyLeDoanhThu { get; set; } // string kèm %
    }
}