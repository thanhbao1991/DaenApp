using System.ComponentModel;

namespace TraSuaApp.Domain.Entities;

public partial class HoaDon
{
    public DateTime? NgayNo { get; set; }

    public Guid Id { get; set; }
    [DefaultValue(false)]

    public DateTime? NgayShip { get; set; }
    public DateTime? NgayIn { get; set; }
    public string? GhiChu { get; set; }
    public string? NguoiShip { get; set; }

    

    public decimal TongTien { get; set; }

    public decimal GiamGia { get; set; }

    public decimal ThanhTien { get; set; }


    public Guid? KhachHangId { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public string? MaHoaDon { get; set; }

    public string? GhiChuShipper { get; set; }

    public Guid? VoucherId { get; set; }

    public string? TenKhachHangText { get; set; }
    public string? DiaChiText { get; set; }

    public string? SoDienThoaiText { get; set; }

    public string TenBan { get; set; } = null!;


    public string? PhanLoai { get; set; }

    public DateTime Ngay { get; set; }

    public DateTime NgayGio { get; set; }

    public virtual ICollection<ChiTietHoaDonPoint> ChiTietHoaDonPoints { get; set; } = new List<ChiTietHoaDonPoint>();

    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();

    public virtual ICollection<ChiTietHoaDonTopping> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonTopping>();

    public virtual ICollection<ChiTietHoaDonVoucher> ChiTietHoaDonVouchers { get; set; } = new List<ChiTietHoaDonVoucher>();

    public virtual ICollection<ChiTietHoaDonEntity> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDonEntity>();

    public virtual KhachHang? KhachHang { get; set; }









}
