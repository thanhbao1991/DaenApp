using System.ComponentModel;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos;

public class ChiTietHoaDonDto : DtoBase, INotifyPropertyChanged
{
    public DateTime NgayGio { get; set; }
    private string? _dinhLuong;

    public string? DinhLuong
    {
        get => _dinhLuong;
        set
        {
            if (_dinhLuong != value)
            {
                _dinhLuong = value;
                OnPropertyChanged(nameof(DinhLuong));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public Guid HoaDonId { get; set; }       // ✅ Thêm thuộc tính này
    public Guid SanPhamIdBienThe { get; set; }
    public int SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public required string TenSanPham { get; set; }
    public required string TenBienThe { get; set; }
    public string? ToppingText { get; set; }
    public string? NoteText { get; set; }
    public override string TimKiem =>
        TextSearchHelper.NormalizeText($"{TenSanPham} {TenBienThe} {TenSanPham.Replace(" ", "")}") + " " +
        TextSearchHelper.GetShortName(TenSanPham ?? "");

    public virtual List<SanPhamBienTheDto> BienTheList { get; set; } = new List<SanPhamBienTheDto>();
    public override string ApiRoute => "ChiTietHoaDon";

    public virtual List<ToppingDto> ToppingDtos { get; set; } = new List<ToppingDto>();

    public decimal TongTienTopping => ToppingDtos?.Sum(x => x.Gia * x.SoLuong) ?? 0;
    public decimal ThanhTien => (DonGia * SoLuong) + TongTienTopping;
}

