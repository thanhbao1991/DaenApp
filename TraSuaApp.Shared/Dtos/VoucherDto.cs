using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class VoucherDto : DtoBase
{
    public override string ApiRoute => "Voucher";

    public string KieuGiam { get; set; } = null!;

    public decimal GiaTri { get; set; }

    public decimal? DieuKienToiThieu { get; set; }  // 🟟 THÊM: Điều kiện hóa đơn tối thiểu

    public int? SoLanSuDungToiDa { get; set; }      // 🟟 TUỲ CHỌN: giới hạn số lượt dùng

    public DateTime NgayBatDau { get; set; }

    public DateTime? NgayKetThuc { get; set; }

    public bool DangSuDung { get; set; }
    public virtual List<Guid> NhomSanPhamIds { get; set; } = new List<Guid>();
    public string TimKiem =>
       $"{Ten?.ToLower() ?? ""} " +
       StringHelper.NormalizeText(Ten ?? "") + " " +
       StringHelper.NormalizeText((Ten ?? "").Replace(" ", "")) + " " +
       StringHelper.GetShortName(Ten ?? "");

}

