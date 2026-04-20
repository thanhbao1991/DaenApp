
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Infrastructure.Dtos;

public class TuDienTraCuuDto : DtoBase
{
    public override string ApiRoute => "TuDienTraCuu";
    public string TenPhienDich { get; set; } = null!;

    

    public int Stt { get; set; }


    public bool DangSuDung { get; set; }
    public string Ten { get; set; } = null!;

    public string TimKiem =>
        $"{Ten?.ToLower() ?? ""} " +
        StringHelper.MyNormalizeText(Ten ?? "") + " " +
        StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
        StringHelper.GetShortName(Ten ?? "");

}

