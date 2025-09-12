using System.ComponentModel.DataAnnotations.Schema;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class KhachHangDto : DtoBase
{
    public override string ApiRoute => "KhachHang";
    public int? OldId { get; set; }
    public int ThuTu { get; set; }
    public bool DuocNhanVoucher { get; set; }
    [NotMapped]
    public string[] TimKiemTokens
    {
        get
        {
            var tokens = new List<string>();

            var ten = TextSearchHelper.NormalizeText(Ten ?? "");
            var tenNoSpace = TextSearchHelper.NormalizeText((Ten ?? "").Replace(" ", ""));
            var reversed = string.Join(" ", ten.Split(' ', StringSplitOptions.RemoveEmptyEntries).Reverse());

            tokens.Add(ten);
            tokens.Add(tenNoSpace);
            tokens.Add(reversed);

            if (!string.IsNullOrWhiteSpace(DienThoai))
                tokens.Add(TextSearchHelper.NormalizeText(DienThoai));

            if (!string.IsNullOrWhiteSpace(DiaChi))
                tokens.AddRange(TextSearchHelper.NormalizeText(DiaChi)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries));

            if (!string.IsNullOrWhiteSpace(TimKiem))
                tokens.Add(TextSearchHelper.NormalizeText(TimKiem));

            return tokens.Where(t => !string.IsNullOrEmpty(t)).Distinct().ToArray();
        }
    }


    public virtual List<KhachHangPhoneDto> Phones { get; set; } = new List<KhachHangPhoneDto>();
    public virtual List<KhachHangAddressDto> Addresses { get; set; } = new List<KhachHangAddressDto>();
    public string? DienThoai => Phones.FirstOrDefault(p => p.IsDefault)?.SoDienThoai ?? string.Empty;
    public string? DiaChi => Addresses.FirstOrDefault(a => a.IsDefault)?.DiaChi ?? string.Empty;

    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        TextSearchHelper.NormalizeText(Ten ?? "") + " " +
        TextSearchHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        TextSearchHelper.GetShortName(Ten ?? "");


    // 🟟 Danh sách địa chỉ



    public override string ToString()
    {
        return $"{Ten}";
    }

}

