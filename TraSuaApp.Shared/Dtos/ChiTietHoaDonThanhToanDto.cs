using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class ChiTietHoaDonThanhToanDto : DtoBase
{
    public override string ApiRoute => "ChiTietHoaDonThanhToan";

    public bool IsToday => Ngay == DateTime.Today;


    public string LoaiThanhToan { get; set; }
    public Guid? ChiTietHoaDonNoId { get; set; }


    public decimal SoTien { get; set; }
    public DateTime NgayGio { get; set; }
    public DateTime Ngay { get; set; }
    public Guid HoaDonId { get; set; }
    public Guid? KhachHangId { get; set; }

    public Guid PhuongThucThanhToanId { get; set; } // ✅ bổ sung

    // Thông tin hiển thị
    public string TenPhuongThucThanhToan { get; set; } = null!;
    public string? GhiChu { get; set; }

    public string TimKiem =>
    $"{Ten?.ToLower() ?? ""} " +
    TextSearchHelper.NormalizeText(Ten ?? "") + " " +
    TextSearchHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
    TextSearchHelper.GetShortName(Ten ?? "");

}