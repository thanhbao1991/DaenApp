using System.Collections.ObjectModel;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class HoaDonDto : DtoBase
{
    public DateTime? NgayNo { get; set; }

    public override string Ten =>
        KhachHangId == null
            ? (TenBan ?? "")
            : (TenKhachHangText ?? "");
    public string? TenBan { get; set; }

    public bool IsThanhToanHidden { get; set; }
    public decimal TongNoKhachHang { get; set; }
    public decimal TongDonKhacDangGiao { get; set; }
    public int DiemThangNay { get; set; }
    public int DiemThangTruoc { get; set; }
    public DateTime? NgayShip { get; set; }
    public string? NguoiShip { get; set; }
    public DateTime? NgayRa { get; set; }
    public string? PhanLoai { get; set; }
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
    public ICollection<ChiTietHoaDonVoucherDto>? ChiTietHoaDonVouchers { get; set; }
    public virtual KhachHang? KhachHang { get; set; }
    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();
    public bool DaNhanVoucher { get; set; }
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(Ten ?? "") + " " +
        StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");
}