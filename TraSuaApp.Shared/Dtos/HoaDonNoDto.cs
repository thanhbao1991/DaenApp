using TraSuaApp.Shared.Helpers;

public class HoaDonNoDto
{
    public Guid Id { get; set; }
    public Guid HoaDonId { get; set; }

    public DateTime? NgayNo { get; set; }
    public DateTime? NgayGio { get; set; }

    public decimal ThanhTien { get; set; }

    public decimal DaThu { get; set; }

    public decimal ConLai { get; set; }

    public Guid? KhachHangId { get; set; }

    public string? TenKhachHangText { get; set; }
    public string? GhiChu { get; set; }
    public int Stt { get; set; }

    public string TimKiem =>
        $"{TenKhachHangText?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(TenKhachHangText ?? "") + " " +
        StringHelper.MyNormalizeText(NgayGio.Value.ToString("dd-MM-yyyy") ?? "") + " " +
        StringHelper.MyNormalizeText((TenKhachHangText ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(TenKhachHangText ?? "");

}