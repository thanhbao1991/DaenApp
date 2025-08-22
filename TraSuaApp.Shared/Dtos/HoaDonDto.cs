using System.ComponentModel;
using TraSuaApp.Domain.Entities;

namespace TraSuaApp.Shared.Dtos;

public class HoaDonDto : DtoBase, INotifyPropertyChanged
{
    [DefaultValue(false)]
    public bool UuTien { get; set; }
    [DefaultValue(false)]
    public bool BaoDon { get; set; }
    public decimal TongNoKhachHang { get; set; }
    public int TongDiem { get; set; }
    public int DiemTrongThang { get; set; }

    public DateTime? NgayShip { get; set; }
    public DateTime? NgayRa { get; set; }

    public string GioHienThi
    {
        get
        {
            if (TrangThai == "Chưa thu")
            {
                var phut = (int)(DateTime.Now - NgayGio).TotalMinutes;
                return $"{phut}'";
            }
            return NgayGio.ToString("hh:mm tt");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // Hàm gọi mỗi phút để refresh cell
    public void RefreshGioHienThi() => OnPropertyChanged(nameof(GioHienThi));



    public string? PhanLoai { get; set; }
    public override string ApiRoute => "HoaDon";
    public DateTime Ngay { get; set; }
    public DateTime NgayGio { get; set; }

    public Guid? KhachHangId { get; set; }
    public Guid? VoucherId { get; set; }

    public string? MaHoaDon { get; set; }
    public string? TenBan { get; set; }
    public string? TrangThai { get; set; }

    public string? DiaChiText { get; set; }
    public string? SoDienThoaiText { get; set; }
    public string? TenKhachHangText { get; set; }
    public string? GhiChu { get; set; }

    public decimal TongTien { get; set; }
    public decimal GiamGia { get; set; }
    public decimal ThanhTien { get; set; }
    public decimal DaThu { get; set; }
    public decimal ConLai { get; set; }

    public int TichDiem => (int)ThanhTien / 10000;
    public virtual ICollection<ChiTietHoaDonDto> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDonDto>();
    public virtual ICollection<ChiTietHoaDonToppingDto> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonToppingDto>();
    public ICollection<ChiTietHoaDonVoucherDto>? ChiTietHoaDonVouchers { get; set; }


    public virtual KhachHang? KhachHang { get; set; }

    // public virtual ICollection<ChiTietHoaDonNo> ChiTietHoaDonNosp { get; set; } = new List<ChiTietHoaDonNo>();

    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();
    public DateTime? NgayHen { get; set; }
}


