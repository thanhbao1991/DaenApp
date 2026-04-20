using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Infrastructure.Dtos;

public class PhuongThucThanhToanDto : DtoBase
{
    
    public int Stt { get; set; }
    public string Ten { get; set; } = null!; public override string ApiRoute => "PhuongThucThanhToan";

    public bool DangSuDung { get; set; }

    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(Ten ?? "") + " " +
        StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");

}

