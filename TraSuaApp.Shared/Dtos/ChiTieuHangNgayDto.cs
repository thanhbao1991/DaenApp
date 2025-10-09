using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class ChiTieuHangNgayDto : DtoBase
{
    public override string ApiRoute => "ChiTieuHangNgay";
    public decimal SoLuong { get; set; }
    public string? GhiChu { get; set; }


    public bool IsToday => Ngay == DateTime.Today;
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
    public DateTime Ngay { get; set; }
    public DateTime NgayGio { get; set; }
    public Guid NguyenLieuId { get; set; }
    public bool BillThang { get; set; }
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(Ten ?? "") + " " +
        StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");

}