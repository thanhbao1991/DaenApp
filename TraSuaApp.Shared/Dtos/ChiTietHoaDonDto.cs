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

    private int _soLuong;
    public int SoLuong
    {
        get => _soLuong;
        set
        {
            if (_soLuong != value)
            {
                _soLuong = value;
                OnPropertyChanged(nameof(SoLuong));
                OnPropertyChanged(nameof(ThanhTien));
            }
        }
    }

    private decimal _donGia;
    public decimal DonGia
    {
        get => _donGia;
        set
        {
            if (_donGia != value)
            {
                _donGia = value;
                OnPropertyChanged(nameof(DonGia));
                OnPropertyChanged(nameof(ThanhTien));
            }
        }
    }

    private string? _noteText;
    public string? NoteText
    {
        get => _noteText;
        set
        {
            if (_noteText != value)
            {
                _noteText = value;
                OnPropertyChanged(nameof(NoteText));
            }
        }
    }


    public required string TenSanPham { get; set; }
    public required string TenBienThe { get; set; }

    public string? ToppingText { get; set; }

    public Guid HoaDonId { get; set; }
    public Guid SanPhamIdBienThe { get; set; }

    public override string TimKiem =>
        TextSearchHelper.NormalizeText($"{TenSanPham} {TenBienThe} {TenSanPham.Replace(" ", "")}") + " " +
        TextSearchHelper.GetShortName(TenSanPham ?? "");

    public virtual List<SanPhamBienTheDto> BienTheList { get; set; } = new List<SanPhamBienTheDto>();
    public virtual List<ToppingDto> ToppingDtos { get; set; } = new List<ToppingDto>();

    public override string ApiRoute => "ChiTietHoaDon";

    public decimal TongTienTopping => ToppingDtos?.Sum(x => x.Gia * x.SoLuong) ?? 0;

    public decimal ThanhTien => (DonGia * SoLuong) + TongTienTopping;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}