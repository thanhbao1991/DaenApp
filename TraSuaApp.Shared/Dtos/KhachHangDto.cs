namespace TraSuaApp.Shared.Dtos;

public class KhachHangDto
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public bool DuocNhanVoucher { get; set; }

    public List<CustomerPhoneNumberDto> PhoneNumbers { get; set; } = new();
    public List<ShippingAddressDto> ShippingAddresses { get; set; } = new();

    public int STT { get; set; }
    public string? TenNormalized { get; set; }
}