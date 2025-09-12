using System.ComponentModel.DataAnnotations.Schema;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class SanPhamDto : DtoBase
{
    public override string ApiRoute => "SanPham";

    public string? DinhLuong { get; set; }
    public string? VietTat { get; set; }
    public int DaBan { get; set; }
    public bool TichDiem { get; set; }
    public bool NgungBan { get; set; }
    public Guid? NhomSanPhamId { get; set; }
    public string? TenNhomSanPham { get; set; }
    [NotMapped]
    public int OldId { get; set; }

    [NotMapped]
    public string[] TimKiemTokens
    {
        get
        {
            var ten = TextSearchHelper.NormalizeText(Ten ?? "");
            var words = ten.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var reversed = string.Join(" ", words.Reverse());

            return new[]
            {
            TextSearchHelper.NormalizeText(VietTat ?? ""),      // viết tắt
            TextSearchHelper.GetShortName(Ten ?? "") ?? "",     // shortname
            ten,                                                // tên đầy đủ
       //     TextSearchHelper.NormalizeText(Ten?.Replace(" ", "") ?? ""), // không dấu cách
            reversed                                            // đảo ngược
        };
        }
    }


    public virtual List<SanPhamBienTheDto> BienThe { get; set; } = new List<SanPhamBienTheDto>();
    public NhomSanPhamDto? NhomSanPham { get; set; }
    public virtual List<SanPhamBienTheDto> SanPhamBienThes { get; set; } = new List<SanPhamBienTheDto>();
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        TextSearchHelper.NormalizeText(Ten ?? "") + " " +
        TextSearchHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        TextSearchHelper.GetShortName(Ten ?? "");

}


