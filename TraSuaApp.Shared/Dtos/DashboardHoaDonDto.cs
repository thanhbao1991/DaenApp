namespace TraSuaApp.Shared.Dtos
{
    public class DashboardHoaDonDto
    {
        public Guid Id { get; set; }
        public string MaHoaDon { get; set; } = "";
        public string TenBan { get; set; } = "";
        public string TrangThai { get; set; } = "";
        public decimal ThanhTien { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime? NgayGio { get; set; }   // ✅ Thêm dòng này

    }
}