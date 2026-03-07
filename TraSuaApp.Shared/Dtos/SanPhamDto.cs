using System.ComponentModel.DataAnnotations.Schema;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class SanPhamDto : DtoBase
{
    [NotMapped]
    public string GiaHienThi
    {
        get
        {
            if (BienThe == null || BienThe.Count == 0)
                return string.Empty;

            var prices = BienThe
                .Select(x => x.GiaBan)
                .OrderBy(x => x)
                .ToList();

            var min = prices.First();
            var max = prices.Last();

            if (min == max)
                return min.ToString("N0");

            return $"{min:N0} - {max:N0}";
        }
    }
    public override string ApiRoute => "SanPham";
    public string? TenKhongVietTat { get; set; }

    public string? DinhLuong { get; set; }
    public string? VietTat { get; set; }
    public int ThuTu { get; set; }
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
            var ten = StringHelper.MyNormalizeText(Ten ?? "");
            var words = ten.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var reversed = string.Join(" ", words.Reverse());

            return new[]
            {
            StringHelper.MyNormalizeText(VietTat ?? ""),      // viết tắt
            StringHelper.GetShortName(Ten ?? "") ?? "",     // shortname
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
        StringHelper.MyNormalizeText(Ten ?? "") + " " +
        StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "") + " " +

        // 🟟 thêm nhóm sản phẩm
        $"{TenNhomSanPham?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(TenNhomSanPham ?? "") + " " +
        StringHelper.MyNormalizeText((TenNhomSanPham ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(TenNhomSanPham ?? "") +

       $"{VietTat?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(VietTat ?? "") + " " +
        StringHelper.MyNormalizeText((VietTat ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(VietTat ?? "");

}


