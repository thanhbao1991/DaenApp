using System.ComponentModel.DataAnnotations.Schema;
using TraSuaApp.Shared.Helpers;

public class HoaDonNoDto
{


    public Guid Id { get; set; }
    public Guid? KhachHangId { get; set; }
    public Guid? VoucherId { get; set; }

    public DateTime? NgayNo { get; set; }
    public DateTime? NgayGio { get; set; }
    public DateTime? NgayShip { get; set; }

    public decimal ThanhTien { get; set; }
    public decimal DaThu { get; set; }
    public decimal ConLai { get; set; }

    public string? TenKhachHangText { get; set; }
    public string? GhiChu { get; set; }
    public string? GhiChuShipper { get; set; }
    public string? NguoiShip { get; set; }
    public string PhanLoai { get; set; }
    [NotMapped]
    public int Stt { get; set; }

    public string TimKiem =>
        $"{TenKhachHangText?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(TenKhachHangText ?? "") + " " +
        StringHelper.MyNormalizeText(NgayGio?.ToString("dd-MM-yyyy") ?? "") + " " +
        StringHelper.MyNormalizeText((TenKhachHangText ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(TenKhachHangText ?? "");

    public bool ChuaCoShipper => string.IsNullOrWhiteSpace(NguoiShip);

    public int WaitingMinutes
    {
        get
        {
            if (!NgayGio.HasValue)
                return 0;

            // ship đã nhận → số phút cố định
            if (NgayShip.HasValue)
                return Math.Max(0, (int)(NgayShip.Value - NgayGio.Value).TotalMinutes);

            // chưa nhận → realtime
            return Math.Max(0, (int)(DateTime.Now - NgayGio.Value).TotalMinutes);
        }
    }

    public int SortOrder
    {
        get
        {
            if (ConLai <= 0)
                return 6;

            if (NgayNo != null)
                return 7;

            if (PhanLoai == "Ship" && ChuaCoShipper)
                return 1;

            if (PhanLoai == "Ship" && !ChuaCoShipper)
                return 5;

            if (PhanLoai == "Mv")
                return 2;

            if (PhanLoai == "App")
                return 3;

            return 4;
        }
    }
}