public class KhachHangAddressDto
{
    public Guid Id { get; set; }
    public Guid IdKhachHang { get; set; }
    public string DiaChi { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}