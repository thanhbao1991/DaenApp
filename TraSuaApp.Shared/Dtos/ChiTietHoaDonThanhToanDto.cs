using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class ChiTietHoaDonThanhToanDto : DtoBase
{
    public override string ApiRoute => "ChiTietHoaDonThanhToan";


    public override string TimKiem =>
    string.Join(" ", new[] {
        Ten,
        Ten?.Replace(" ", ""),
        TenPhuongThucThanhToan,
        TenPhuongThucThanhToan?.Replace(" ", ""),
        LoaiThanhToan,
        LoaiThanhToan?.Replace(" ", "")
    }
    .Where(s => !string.IsNullOrEmpty(s))
    .Select(s => TextSearchHelper.NormalizeText(s))
    ) + " " + TextSearchHelper.GetShortName(Ten ?? "");

    public string LoaiThanhToan { get; set; }
    public Guid? ChiTietHoaDonNoId { get; set; }


    public decimal SoTien { get; set; }
    public DateTime NgayGio { get; set; }
    public DateTime Ngay { get; set; }
    public Guid HoaDonId { get; set; }
    public Guid? KhachHangId { get; set; }

    public Guid PhuongThucThanhToanId { get; set; } // ✅ bổ sung

    // Thông tin hiển thị
    public string? TenPhuongThucThanhToan { get; set; }
    public string? GhiChu { get; set; }
}