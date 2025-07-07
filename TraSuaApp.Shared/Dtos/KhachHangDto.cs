namespace TraSuaApp.Shared.Dtos;

public class KhachHangDto
{
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public bool DuocNhanVoucher { get; set; }

    // 🟟 TÊN CHUẨN: Phones (không dùng Phones)
    public List<KhachHangPhoneDto> Phones { get; set; } = new();
    public List<KhachHangAddressDto> Addresses { get; set; } = new();

    // 🟟 UI hỗ trợ
    public int STT { get; set; }
    public string? TenNormalized { get; set; }

    public KhachHangPhoneDto? DefaultPhone => Phones.FirstOrDefault(p => p.IsDefault);
    public KhachHangAddressDto? DefaultAddress => Addresses.FirstOrDefault(a => a.IsDefault);
}