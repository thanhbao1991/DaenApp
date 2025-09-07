using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class ChiTietHoaDonThanhToanDto : DtoBase
{
    public override string ApiRoute => "ChiTietHoaDonThanhToan";

    public bool IsToday => Ngay == DateTime.Today;

    public override string TimKiem =>
    string.Join(" ", new[] {
        Ten,
        TenPhuongThucThanhToan,
        LoaiThanhToan,
        GhiChu,

        Ten?.Replace(" ", ""),
        TenPhuongThucThanhToan?.Replace(" ", ""),
        LoaiThanhToan?.Replace(" ", ""),
        GhiChu?.Replace(" ", ""),
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
    public string TenPhuongThucThanhToan { get; set; } = null!;
    public string? GhiChu { get; set; }
}