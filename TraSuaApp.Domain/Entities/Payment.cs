namespace TraSuaApp.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public Guid IdHoaDon { get; set; }
    public Guid IdPaymentMethod { get; set; }
    public decimal SoTien { get; set; }
    public DateTime NgayThanhToan { get; set; }
    public Guid? IdTaiKhoanThucHien { get; set; }

    public HoaDon HoaDon { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public TaiKhoan? TaiKhoanThucHien { get; set; }
}