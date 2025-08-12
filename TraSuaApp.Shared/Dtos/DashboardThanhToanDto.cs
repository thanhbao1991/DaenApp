namespace TraSuaApp.Shared.Dtos
{
    public class DashboardThanhToanDto
    {
        public string Ten { get; set; } = "";
        public string TenPhuongThuc { get; set; } = "";

        public decimal SoTien { get; set; }
        public DateTime NgayGio { get; set; }
    }
}