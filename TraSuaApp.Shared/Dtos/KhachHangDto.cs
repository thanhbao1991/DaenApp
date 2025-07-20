using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

public class KhachHangDto : DtoBase
{
    public override string ApiRoute => "KhachHang";
    public int? OldId { get; set; }

    public bool DuocNhanVoucher { get; set; }

    public List<KhachHangPhoneDto> Phones { get; set; } = new();
    public List<KhachHangAddressDto> Addresses { get; set; } = new();

    public override string TimKiem =>
        TextSearchHelper.NormalizeText($"{Ten} {DefaultAddress} {DefaultPhone}") + " " +
        TextSearchHelper.GetShortName(Ten ?? "") + " " +
        TextSearchHelper.GetShortName(DefaultAddress ?? "");

    public string? DefaultPhone => Phones.FirstOrDefault(p => p.IsDefault)?.SoDienThoai ?? string.Empty;
    public string? DefaultAddress => Addresses.FirstOrDefault(a => a.IsDefault)?.DiaChi ?? string.Empty;

}