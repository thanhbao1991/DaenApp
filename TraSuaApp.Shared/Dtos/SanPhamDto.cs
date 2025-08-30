using System.ComponentModel.DataAnnotations.Schema;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class SanPhamDto : DtoBase
{
    public override string ApiRoute => "SanPham";

    public string? DinhLuong { get; set; }
    public string? VietTat { get; set; }
    public int? DaBan { get; set; }
    public bool TichDiem { get; set; }
    public bool NgungBan { get; set; }
    public Guid? NhomSanPhamId { get; set; }
    public string? TenNhomSanPham { get; set; }
    [NotMapped]
    public int OldId { get; set; }
    public override string TimKiem =>
    TextSearchHelper.NormalizeText($"{Ten} {Ten.Replace(" ", "")} {VietTat}") + " " +
    TextSearchHelper.GetShortName(Ten ?? "");

    public virtual List<SanPhamBienTheDto> BienThe { get; set; } = new List<SanPhamBienTheDto>();
    public NhomSanPhamDto? NhomSanPham { get; set; }
    public virtual List<SanPhamBienTheDto> SanPhamBienThes { get; set; } = new List<SanPhamBienTheDto>();
}


