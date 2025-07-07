using System;
using System.Collections.Generic;

namespace TraSuaApp.Domain.Entities;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid IdHoaDon { get; set; }

    public Guid IdPaymentMethod { get; set; }

    public decimal SoTien { get; set; }

    public DateTime NgayThanhToan { get; set; }

    public Guid? IdTaiKhoanThucHien { get; set; }

    public Guid HoaDonId { get; set; }

    public Guid PaymentMethodId { get; set; }

    public Guid? TaiKhoanThucHienId { get; set; }

    public virtual HoaDon HoaDon { get; set; } = null!;

    public virtual PaymentMethod PaymentMethod { get; set; } = null!;

    public virtual TaiKhoan? TaiKhoanThucHien { get; set; }
}
