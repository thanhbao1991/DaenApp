public class KhachHangPhoneDto
{
    public Guid Id { get; set; }
    public Guid KhachHangId { get; set; }
    public string SoDienThoai { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
}
