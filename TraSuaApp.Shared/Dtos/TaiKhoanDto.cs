using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class TaiKhoanDto : DtoBase
{
    public override string ApiRoute => "TaiKhoan";

    public string TenDangNhap { get; set; } = default!;
    public string? MatKhau { get; set; } = default!;

    public string? TenHienThi { get; set; }
    public string? VaiTro { get; set; }
    public bool IsActive { get; set; }
    public string TimKiem =>
     $"{Ten?.ToLower() ?? ""} " +
     StringHelper.MyNormalizeText(Ten ?? "") + " " +
     StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
     StringHelper.GetShortName(Ten ?? "");

}
