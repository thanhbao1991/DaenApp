using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class HoaDon
{
    public Guid Id { get; set; }

    public DateTime NgayTao { get; set; }

    public Guid? IdKhachHang { get; set; }

    public Guid? IdBan { get; set; }

    public Guid? IdTaiKhoan { get; set; }

    public Guid? IdNhomHoaDon { get; set; }

    public decimal TongTien { get; set; }

    public decimal GiamGia { get; set; }

    public decimal ThanhTien { get; set; }

    public Guid? KhachHangId { get; set; }

    public Guid? TaiKhoanId { get; set; }

    public virtual ICollection<ChiTietHoaDonTopping> ChiTietHoaDonToppings { get; set; } = new List<ChiTietHoaDonTopping>();

    public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; } = new List<ChiTietHoaDon>();

    public virtual KhachHang? KhachHang { get; set; }

    public virtual ICollection<NoHoaDon> NoHoaDons { get; set; } = new List<NoHoaDon>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual TaiKhoan? TaiKhoan { get; set; }

    public virtual ICollection<VoucherLog> VoucherLogs { get; set; } = new List<VoucherLog>();
}
