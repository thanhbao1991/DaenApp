using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Infrastructure.Dtos;

public class CongViecNoiBoDto : DtoBase
{

    
    
    
    public int Stt { get; set; }
    public string Ten { get; set; } = null!;
    
     public override string ApiRoute => "CongViecNoiBo";

    public bool DaHoanThanh { get; set; }
    public DateTime? NgayGio { get; set; }

    // ✅ Trường mới cảnh báo
    public DateTime? NgayCanhBao { get; set; }
    public int? XNgayCanhBao { get; set; }

    // ✅ Metadata đồng bộ với Entity

    public string TimKiem =>
       $"{Ten?.ToLower() ?? ""} " +
       StringHelper.MyNormalizeText(Ten ?? "") + " " +
       StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
       StringHelper.GetShortName(Ten ?? "");

}