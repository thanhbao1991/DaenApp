namespace TraSuaApp.Shared.Dtos;

public class CongViecNoiBoDto : DtoBase
{
    public override string ApiRoute => "CongViecNoiBo";
    public bool DaHoanThanh { get; set; }
    public DateTime? NgayGio { get; set; }   // ✅ Thêm dòng này

}

