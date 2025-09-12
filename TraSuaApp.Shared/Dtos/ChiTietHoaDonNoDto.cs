using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class ChiTietHoaDonNoDto : DtoBase
{
    //    public override string TimKiem =>
    //string.Join(" ", new[] {
    //        Ten,
    //        Ten?.Replace(" ", ""),
    //        Ngay.ToString("dd-MM-yyyy")
    //}
    //.Where(s => !string.IsNullOrEmpty(s))
    //.Select(s => TextSearchHelper.NormalizeText(s))
    //) + " " + TextSearchHelper.GetShortName(Ten ?? "");
    public bool IsToday => Ngay == DateTime.Today;


    public override string ApiRoute => "ChiTietHoaDonNo";
    public decimal SoTienNo { get; set; }
    public decimal SoTienDaTra { get; set; }
    public decimal ConLai => SoTienNo - SoTienDaTra;
    public DateTime NgayGio { get; set; }
    public DateTime Ngay { get; set; }


    public string? MaHoaDon { get; set; }
    public string? GhiChu { get; set; }

    public Guid HoaDonId { get; set; }
    public Guid? KhachHangId { get; set; }
    public string TimKiem =>
    $"{Ten?.ToLower() ?? ""} " +
    TextSearchHelper.NormalizeText(Ten ?? "") + " " +
    TextSearchHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
    TextSearchHelper.GetShortName(Ten ?? "");

}