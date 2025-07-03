namespace TraSuaApp.Domain.Entities;

public class Voucher
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public decimal GiaTri { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime? NgayKetThuc { get; set; }
    public bool DangSuDung { get; set; } = true;

    public ICollection<VoucherLog> VoucherLogs { get; set; }
}