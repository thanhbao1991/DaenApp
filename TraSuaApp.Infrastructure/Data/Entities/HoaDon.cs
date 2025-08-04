using System;
using System.Collections.Generic;

namespace TraSuaApp.Infrastructure.Data.Entities;

public partial class HoaDon
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal TongTien { get; set; }

    public decimal GiamGia { get; set; }

    public decimal ThanhTien { get; set; }

    public Guid? KhachHangId { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? LastModified { get; set; }

    public string? MaHoaDon { get; set; }

    public string? TrangThai { get; set; }

    public Guid? VoucherId { get; set; }

    public string? DiaChiText { get; set; }

    public string? SoDienThoaiText { get; set; }

    public string? TenBan { get; set; }

    public int OldId { get; set; }

    public string? PhanLoai { get; set; }

    public DateTime Ngay { get; set; }

    public DateTime NgayGio { get; set; }

    public virtual ICollection<ChiTietHoaDonNo> ChiTietHoaDonNos { get; set; } = new List<ChiTietHoaDonNo>();

    public virtual ICollection<ChiTietHoaDonThanhToan> ChiTietHoaDonThanhToans { get; set; } = new List<ChiTietHoaDonThanhToan>();

    public virtual ICollection<ChiTietHoaDonTopping> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonTopping>();

    public virtual ICollection<ChiTietHoaDonVoucher> ChiTietHoaDonVouchers { get; set; } = new List<ChiTietHoaDonVoucher>();

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual KhachHang? KhachHang { get; set; }
}
