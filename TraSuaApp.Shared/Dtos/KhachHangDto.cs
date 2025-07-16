using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Providers;

public class KhachHangDto : IHasId, IHasRoute
{
    public string ApiRoute => "khachhang";

    public DateTime CreatedAt { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid Id { get; set; }
    public string Ten { get; set; } = string.Empty;
    public DateTime? NgaySinh { get; set; }
    public string? GioiTinh { get; set; }
    public bool DuocNhanVoucher { get; set; }

    public List<KhachHangPhoneDto> Phones { get; set; } = new();
    public List<KhachHangAddressDto> Addresses { get; set; } = new();

    public int STT { get; set; }
    public string? TenNormalized => TextSearchHelper.NormalizeText(Ten ?? "");
    public string? DefaultAddressNormalized => TextSearchHelper.NormalizeText(DefaultAddress ?? "");


    public string? DefaultPhone => Phones.FirstOrDefault(p => p.IsDefault)?.SoDienThoai ?? string.Empty;
    public string? DefaultAddress => Addresses.FirstOrDefault(a => a.IsDefault)?.DiaChi ?? string.Empty;

}