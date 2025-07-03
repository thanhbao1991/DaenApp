namespace TraSuaApp.Domain.Entities;

public class KhachHang
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public bool DuocNhanVoucher { get; set; } = true;

    public ICollection<ShippingAddress> ShippingAddresses { get; set; }
    public ICollection<CustomerPhoneNumber> PhoneNumbers { get; set; }
}