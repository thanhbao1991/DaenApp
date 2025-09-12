using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class HoaDonDto : DtoBase, INotifyPropertyChanged
{


    [NotMapped]
    public string RowBackground
    {
        get
        {
            if (PhanLoai == "Ship")
            {
                if (NgayShip == null)
                    return "Blue"; // chưa đi ship
                if (DaThuHoacGhiNo == false)
                    return "Transparent"; // đi ship + chưa thu
                else
                {
                    if (ConLai > 0)
                        return "Transparent";
                }
                return "Transparent"; // đi ship + đã thu
            }
            else
            {
                if (DaThuHoacGhiNo == false)
                    return "Blue";
                else
                {
                    if (ConLai > 0)
                        return "Transparent";
                }
                return "Transparent";
            }
        }
    }
    public string RowForeground
    {
        get
        {
            if (PhanLoai == "Ship")
            {
                if (NgayShip == null)
                    return "White"; // chưa đi ship
                if (DaThuHoacGhiNo == false)
                    return "Black"; // đi ship + chưa thu
                else
                {
                    if (ConLai > 0)
                        return "IndianRed";
                }
                return "Black"; // đi ship + đã thu
            }
            else
            {
                if (DaThuHoacGhiNo == false)
                    return "White";
                else
                {
                    if (ConLai > 0)
                        return "IndianRed";
                }
                return "Black";
            }

        }
    }
    public bool IsBlue
    {
        get
        {
            if (PhanLoai == "Ship")
            {
                return NgayShip == null;  // chưa đi ship
            }
            else
            {
                return DaThuHoacGhiNo == false; // chưa thu tại chỗ
            }
        }
    }
    [DefaultValue(false)]
    public bool UuTien { get; set; }
    [DefaultValue(false)]
    public bool BaoDon { get; set; }
    public decimal TongNoKhachHang { get; set; }
    public int TongDiem { get; set; }
    public int DiemThangNay { get; set; }
    public int DiemThangTruoc { get; set; }

    public DateTime? NgayShip { get; set; }
    public string? NguoiShip { get; set; }
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
            return NgayGio.ToString("HH:mm");
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
    public string TenBan { get; set; } = null!;
    public string TrangThai { get; set; } = null!;

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

    public int TichDiem => (int)ThanhTien / 10000;
    public virtual ICollection<ChiTietHoaDonDto> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDonDto>();
    public virtual ICollection<ChiTietHoaDonToppingDto> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonToppingDto>();
    public ICollection<ChiTietHoaDonVoucherDto>? ChiTietHoaDonVouchers { get; set; }

    public virtual KhachHang? KhachHang { get; set; }
    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();
    public DateTime? NgayHen { get; set; }
    public bool DaNhanVoucher { get; set; }
    public bool DaThuHoacGhiNo { get; set; }
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        TextSearchHelper.NormalizeText(Ten ?? "") + " " +
        TextSearchHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        TextSearchHelper.GetShortName(Ten ?? "");


    // 🟟 Hàm đồng bộ dữ liệu khi nhận update từ SignalR
    public void CopyFrom(HoaDonDto other)
    {
        if (other == null) return;

        UuTien = other.UuTien;
        BaoDon = other.BaoDon;
        TongNoKhachHang = other.TongNoKhachHang;
        TongDiem = other.TongDiem;
        DiemThangNay = other.DiemThangNay;
        DiemThangTruoc = other.DiemThangTruoc;

        NgayShip = other.NgayShip;
        NgayRa = other.NgayRa;
        NgayHen = other.NgayHen;

        PhanLoai = other.PhanLoai;
        Ngay = other.Ngay;
        NgayGio = other.NgayGio;

        KhachHangId = other.KhachHangId;
        VoucherId = other.VoucherId;

        MaHoaDon = other.MaHoaDon;
        TenBan = other.TenBan;
        TrangThai = other.TrangThai;

        DiaChiText = other.DiaChiText;
        SoDienThoaiText = other.SoDienThoaiText;
        TenKhachHangText = other.TenKhachHangText;
        GhiChu = other.GhiChu;
        GhiChuShipper = other.GhiChuShipper;

        TongTien = other.TongTien;
        GiamGia = other.GiamGia;
        ThanhTien = other.ThanhTien;
        DaThu = other.DaThu;
        ConLai = other.ConLai;

        // collections
        ChiTietHoaDons = other.ChiTietHoaDons?.ToList() ?? new List<ChiTietHoaDonDto>();
        ChiTietHoaDonToppings = other.ChiTietHoaDonToppings?.ToList() ?? new List<ChiTietHoaDonToppingDto>();
        ChiTietHoaDonVouchers = other.ChiTietHoaDonVouchers?.ToList();
        ChiTietHoaDonThanhToans = other.ChiTietHoaDonThanhToans?.ToList() ?? new List<ChiTietHoaDonThanhToan>();

        KhachHang = other.KhachHang;

        DaNhanVoucher = other.DaNhanVoucher;
        DaThuHoacGhiNo = other.DaThuHoacGhiNo;

        LastModified = other.LastModified;
        DeletedAt = other.DeletedAt;
        IsDeleted = other.IsDeleted;
    }
}