using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class CongViecNoiBoDto : DtoBase
{
    public override string ApiRoute => "CongViecNoiBo";

    public bool DaHoanThanh { get; set; }
    public DateTime? NgayGio { get; set; }

    // ✅ Trường mới cảnh báo
    public DateTime? NgayCanhBao { get; set; }
    public int? XNgayCanhBao { get; set; }

    // ✅ Metadata đồng bộ với Entity
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? LastModified { get; set; }
    public string TimKiem =>
       $"{Ten?.ToLower() ?? ""} " +
       StringHelper.MyNormalizeText(Ten ?? "") + " " +
       StringHelper.MyNormalizeText((Ten ?? "").Replace(" ", "")) + " " +
       StringHelper.GetShortName(Ten ?? "");

}