using TraSuaApp.Domain.Entities;

namespace TraSuaApp.Shared.Dtos;

public class HoaDonDto : DtoBase
{

    public string? PhanLoai { get; set; }

    public override string ApiRoute => "HoaDon";
    public DateTime Ngay { get; set; }
    public DateTime NgayGio { get; set; }

    public Guid? KhachHangId { get; set; }
    public Guid? VoucherId { get; set; }

    public string? MaHoaDon { get; set; }
    public string? TenBan { get; set; }
    public string? TrangThai { get; set; }              // Enum: Chờ, Đã TT, Đã hủy, Nợ
    public string? DiaChiText { get; set; }
    public string? SoDienThoaiText { get; set; }
    public string? TenKhachHang { get; set; }







    public decimal TongTien { get; set; }
    public decimal GiamGia { get; set; }
    public decimal ThanhTien { get; set; }

    public int TichDiem => (int)ThanhTien / 10000;
    public virtual ICollection<ChiTietHoaDonDto> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDonDto>();
    public virtual ICollection<ChiTietHoaDonToppingDto> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonToppingDto>();
    public ICollection<ChiTietHoaDonVoucherDto>? ChiTietHoaDonVouchers { get; set; }


    public virtual KhachHang? KhachHang { get; set; }

    public virtual ICollection<ChiTietHoaDonNo> ChiTietHoaDonNosp { get; set; } = new List<ChiTietHoaDonNo>();

    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();


}


