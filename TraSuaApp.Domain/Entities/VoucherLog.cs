namespace TraSuaApp.Domain.Entities;

public class VoucherLog
{
    public Guid Id { get; set; }
    public Guid IdVoucher { get; set; }
    public Guid IdHoaDon { get; set; }
    public decimal GiaTriApDung { get; set; }

    public Voucher Voucher { get; set; }
    public HoaDon HoaDon { get; set; }
}