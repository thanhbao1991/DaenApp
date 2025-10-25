using System.Collections.ObjectModel;
using System.ComponentModel;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class HoaDonDto : DtoBase, INotifyPropertyChanged
{
    public bool DaThuHoacGhiNo => ConLai == 0m || HasDebt;




    public string RowBackground
    {
        get
        {
            var statusRaw = (TrangThai ?? string.Empty).Trim();
            var statusLower = statusRaw.ToLowerInvariant();
            var isShip = string.Equals(PhanLoai, "Ship", StringComparison.OrdinalIgnoreCase);

            // ✅ Không nền cho các trạng thái sau (mọi phân loại)
            if (
              statusLower.Contains("đã thu") ||
               statusLower.Contains("đã chuyển khoản") ||
               statusLower.Contains("ghi nợ") ||
               statusLower.Contains("nợ một phần") ||
               // "Không thu" nhưng đã đi ship -> không nền
               (statusLower.Equals("không thu") && isShip && NgayShip != null)
                )
            {
                return "Transparent";
            }



            // ✅ Quy tắc đặc biệt cho "Không thu"
            if (statusRaw.Equals("Không thu", StringComparison.OrdinalIgnoreCase) && isShip)
            {
                // Chưa đi ship -> đậm; Đã đi ship -> không nền
                return NgayShip == null ? "DodgerBlue" : "Transparent";
            }

            // ❖ Ship: chưa đi -> xanh dương đậm; đã đi -> xanh dương nhạt
            if (isShip) return NgayShip == null ? "DodgerBlue" : "Transparent";

            if (PhanLoai == "App") return "LightCoral";

            // ❖ Khác Ship: chưa thu -> xanh lá đậm; còn lại -> xanh lá nhạt
            return DaThuHoacGhiNo ? "GreenYellow" : "GreenYellow";
        }
    }

    public string RowForeground
    {
        get
        {
            var statusRaw = (TrangThai ?? string.Empty).Trim();
            var statusLower = statusRaw.ToLowerInvariant();
            var isShip = string.Equals(PhanLoai, "Ship", StringComparison.OrdinalIgnoreCase);

            // 1) Ưu tiên màu chữ theo trạng thái
            if (statusLower.Contains("nợ") && !statusLower.Contains("trả"))
                return "IndianRed";            // đỏ nếu "nợ" (không phải "trả nợ")

            if (statusLower.Contains("chuyển khoản"))
                return "Orange";                 // vàng nếu "chuyển khoản"

            // 2) Xác định nền để chọn đen/trắng
            var noBackground =
                statusLower.Contains("đã thu") ||
                statusLower.Contains("đã chuyển khoản") ||
                statusLower.Contains("ghi nợ") ||
                statusLower.Contains("nợ một phần") ||
                // "Không thu" nhưng đã đi ship -> không nền
                (statusLower.Equals("không thu") && isShip && NgayShip != null);

            // Nền đậm: Ship chưa đi (DodgerBlue) hoặc Không-Ship chưa thu (Green)
            var bgIsDark = !noBackground && ((isShip && NgayShip == null) || (!isShip && !DaThuHoacGhiNo));

            return bgIsDark ? "Black" : "Black";
        }
    }





    [DefaultValue(false)]
    public bool UuTien { get; set; }
    [DefaultValue(false)]
    public bool BaoDon { get; set; }
    public bool HasDebt { get; set; }
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
    public decimal DaThu => ThanhTien - ConLai;
    public decimal ConLai { get; set; }
    public int TichDiem => (int)ThanhTien / 10000;
    public ObservableCollection<ChiTietHoaDonDto> ChiTietHoaDons { get; set; }
     = new ObservableCollection<ChiTietHoaDonDto>();
    public virtual ICollection<ChiTietHoaDonToppingDto> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonToppingDto>();
    public ICollection<ChiTietHoaDonVoucherDto>? ChiTietHoaDonVouchers { get; set; }

    public virtual KhachHang? KhachHang { get; set; }
    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();
    public DateTime? NgayHen { get; set; }
    public bool DaNhanVoucher { get; set; }
    public override string Ten => KhachHangId == null ? TenBan : TenKhachHangText;
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(Ten ?? "") + " " +
        StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");


    public string TenHienThi
    {
        get
        {
            // 1) Có tên khách -> ưu tiên
            if (!string.IsNullOrWhiteSpace(TenKhachHangText))
                return TenKhachHangText;

            // 2) Không có KH -> nếu có bàn (Tại chỗ hay không) thì dùng tên bàn
            if (!string.IsNullOrWhiteSpace(TenBan))
                return TenBan;

            // 3) Fallback theo ngữ cảnh giao/ship
            if (!string.IsNullOrWhiteSpace(DiaChiText))
                return DiaChiText;

            // 4) Cuối cùng rơi về Ten (nếu nơi khác đã gán) hoặc mã HD
            if (!string.IsNullOrWhiteSpace(Ten))
                return Ten;

            return $"HD #{Id}";
        }
    }


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
        ConLai = other.ConLai;

        // collections
        ChiTietHoaDons = other.ChiTietHoaDons;
        ChiTietHoaDonToppings = other.ChiTietHoaDonToppings?.ToList() ?? new List<ChiTietHoaDonToppingDto>();
        ChiTietHoaDonVouchers = other.ChiTietHoaDonVouchers?.ToList();
        ChiTietHoaDonThanhToans = other.ChiTietHoaDonThanhToans?.ToList() ?? new List<ChiTietHoaDonThanhToan>();

        KhachHang = other.KhachHang;

        DaNhanVoucher = other.DaNhanVoucher;

        LastModified = other.LastModified;
        DeletedAt = other.DeletedAt;
        IsDeleted = other.IsDeleted;
    }
}