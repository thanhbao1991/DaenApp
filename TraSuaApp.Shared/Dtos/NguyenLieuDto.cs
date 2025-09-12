using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos
{
    public class NguyenLieuDto : DtoBase
    {
        public override string ApiRoute => "NguyenLieu";


        public string? DonViTinh { get; set; }

        public decimal? TonKho { get; set; }

        public decimal GiaNhap { get; set; }

        public bool DangSuDung { get; set; }
        public string TimKiem =>
     $"{Ten?.ToLower() ?? ""} " +
     TextSearchHelper.NormalizeText(Ten ?? "") + " " +
     TextSearchHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
     TextSearchHelper.GetShortName(Ten ?? "");

    }
}