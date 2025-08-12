namespace TraSuaApp.Shared.Dtos;

public class ChiTietHoaDonThanhToanDto : DtoBase
{
    public override string ApiRoute => "ChiTietHoaDonThanhToan";

    public decimal SoTien { get; set; }
    public DateTime NgayGio { get; set; }
    public DateTime Ngay { get; set; }
    public Guid HoaDonId { get; set; }
    public Guid? KhachHangId { get; set; }

    public Guid PhuongThucThanhToanId { get; set; } // ✅ bổ sung

    // Thông tin hiển thị
    public string? TenPhuongThucThanhToan { get; set; }
}