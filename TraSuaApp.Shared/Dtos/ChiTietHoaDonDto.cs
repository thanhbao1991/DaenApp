namespace TraSuaApp.Shared.Dtos;

public class ChiTietHoaDonDto : DtoBase
{
    public DateTime NgayGio { get; set; }

    public Guid HoaDonId { get; set; }       // ✅ Thêm thuộc tính này
    public Guid SanPhamIdBienThe { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public required string TenSanPham { get; set; }
    public required string TenBienThe { get; set; }
    public string? ToppingText { get; set; }
    public string? NoteText { get; set; }


    public virtual List<SanPhamBienTheDto> BienTheList { get; set; } = new List<SanPhamBienTheDto>();
    public override string ApiRoute => "ChiTietHoaDon";

    public virtual List<ToppingDto> ToppingDtos { get; set; } = new List<ToppingDto>();

    public decimal TongTienTopping => ToppingDtos?.Sum(x => x.Gia * x.SoLuong) ?? 0;
    public decimal ThanhTien => (DonGia * SoLuong) + TongTienTopping;
}

