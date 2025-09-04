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

    [NotMapped]
    public override string TimKiem
    {
        get
        {
            var ten = TextSearchHelper.NormalizeText(Ten ?? "");
            var tenNhom = TextSearchHelper.NormalizeText(TenNhomSanPham ?? "");

            // Ghép tên và tên nhóm lại, ngăn cách bằng khoảng trắng
            return $"{ten} {tenNhom}".Trim();
        }
    }

    public virtual List<SanPhamBienTheDto> BienThe { get; set; } = new List<SanPhamBienTheDto>();
    public NhomSanPhamDto? NhomSanPham { get; set; }
    public virtual List<SanPhamBienTheDto> SanPhamBienThes { get; set; } = new List<SanPhamBienTheDto>();
}


