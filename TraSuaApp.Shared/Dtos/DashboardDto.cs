using TraSuaApp.Shared.Dtos;

public class DashboardDto
{

    public List<DashboardTopSanPhamDto> TopSanPhams { get; set; } = new();
    public string? PredictedPeak { get; set; }

}