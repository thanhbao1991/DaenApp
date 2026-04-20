using System.Collections.ObjectModel;

using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Infrastructure.Dtos;

public class HoaDonDto : DtoBase
{
    public DateTime? NgayNo { get; set; }

    public string? TenBan { get; set; }

    public bool IsThanhToanHidden { get; set; }
    public decimal TongNoKhachHang { get; set; }
    public decimal TongDonKhacDangGiao { get; set; }
    public int DiemThangNay { get; set; }
    public int DiemThangTruoc { get; set; }
    public DateTime? NgayShip { get; set; }
    public string? NguoiShip { get; set; }
    public DateTime? NgayIn { get; set; }
    public string? PhanLoai { get; set; }


    

    public int Stt { get; set; }


    public override string ApiRoute => "HoaDon";
    public DateTime Ngay { get; set; }
    public DateTime NgayGio { get; set; }
    public Guid? KhachHangId { get; set; }
    public Guid? VoucherId { get; set; }
    public string? MaHoaDon { get; set; }
    public string? DiaChiText { get; set; }
    public string? SoDienThoaiText { get; set; }
    public string? TenKhachHangText { get; set; }
    public string? GhiChu { get; set; }
    public string? GhiChuShipper { get; set; }
    public decimal TongTien { get; set; }
    public decimal GiamGia { get; set; }
    public decimal ThanhTien { get; set; }
    public decimal DaThu { get; set; }
    public decimal ConLai { get; set; }
    public ObservableCollection<ChiTietHoaDonDto> ChiTietHoaDons { get; set; }
     = new ObservableCollection<ChiTietHoaDonDto>();
    public virtual ICollection<ChiTietHoaDonToppingDto> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonToppingDto>();
    public bool DaNhanVoucher { get; set; }
    public string TimKiem =>
        $"{TenBan?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(TenKhachHangText ?? "") + " " +
        StringHelper.MyNormalizeText((TenKhachHangText ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(TenKhachHangText ?? "");
}