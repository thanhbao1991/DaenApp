namespace TraSuaApp.Shared.Dtos
{
    public class DashboardTyLeDonDto
    {
        public string LoaiDon { get; set; }
        public int SoLuong { get; set; }
        public decimal DoanhThu { get; set; }   // ✅ Thêm thuộc tính này
    }
}