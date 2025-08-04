namespace TraSuaApp.Shared.Dtos;

public class ChiTietHoaDonVoucherDto : DtoBase
{
    public override string ApiRoute => "VoucherLog";

    public Guid HoaDonId { get; set; }
    public Guid VoucherId { get; set; }
    public decimal GiaTriApDung { get; set; }
}
