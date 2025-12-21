//using System.Collections.ObjectModel;
//using TraSuaApp.Shared.Dtos;

//public class ChiTieuHangNgayEditVm : ViewModelBase
//{
//    public DateTime Ngay { get; set; } = DateTime.Today;
//    public bool BillThang { get; set; }

//    public ObservableCollection<ChiTieuHangNgayDto> Lines { get; }
//        = new();

//    public bool IsBulk => Lines.Count > 1;

//    public decimal TongTien =>
//        Lines.Sum(x => x.ThanhTien ?? 0);

//    public ChiTieuHangNgayEditVm()
//    {
//        Lines.CollectionChanged += (_, __) =>
//        {
//            RaisePropertyChanged(nameof(TongTien));
//            RaisePropertyChanged(nameof(IsBulk));
//        };
//    }
//}