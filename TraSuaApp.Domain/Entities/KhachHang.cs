namespace TraSuaApp.Domain.Entities;

public class KhachHang
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public bool DuocNhanVoucher { get; set; } = true;

    public ICollection<KhachHangAddress> Addresss { get; set; }
    public ICollection<KhachHangPhone> Phones { get; set; }
}