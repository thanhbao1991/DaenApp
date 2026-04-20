using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Infrastructure.Dtos;

public class NguyenLieuBanHangDto : DtoBase
{
    
    public int Stt { get; set; }
    public string Ten { get; set; } = null!; public override string ApiRoute => "NguyenLieuBanHang";

    public bool DangSuDung { get; set; }
    public string? DonViTinh { get; set; }

    /// <summary>
    /// Tồn kho theo đơn vị bán (lon/ml/gram...)
    /// </summary>
    public decimal TonKho { get; set; }


    //
    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(Ten ?? "") + " " +
        StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");

}

