namespace TraSuaApp.Domain.Entities;

public partial class ChiTietHoaDonVoucher : EntityBase
{

    public Guid VoucherId { get; set; }

    public Guid HoaDonId { get; set; }

    public decimal GiaTriApDung { get; set; }

    public required string TenVoucher { get; set; }

    public virtual HoaDon? HoaDon { get; set; }

    public virtual Voucher? Voucher { get; set; }
}




