using TraSuaApp.Shared.Dtos;

public class DashboardDto
{

    public List<TopSanPhamDto> TopSanPhams { get; set; } = new();
    public List<ChiTietHoaDonDto> History { get; set; } = new();

}