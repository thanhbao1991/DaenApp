

using System.ComponentModel;
using System.Runtime.CompilerServices;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Shared.Dtos
{
    public class HoaDonNoDto : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null!)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        protected bool SetField<T>(ref T field, T value, params string[] extraProps)
        {
            if (Equals(field, value)) return false;

            field = value;
            OnPropertyChanged();

            foreach (var prop in extraProps)
                OnPropertyChanged(prop);

            return true;
        }

        // ================== BASIC ==================

        public Guid Id { get; set; }

        private string? _tenKhachHangText;
        public string? TenKhachHangText
        {
            get => _tenKhachHangText;
            set => SetField(ref _tenKhachHangText, value, nameof(TimKiem));
        }

        //private string? _diaChiText;
        //public string? DiaChiText
        //{
        //    get => _diaChiText;
        //    set => SetField(ref _diaChiText, value, nameof(TimKiem));
        //}

        private Guid? _khachHangId;
        public Guid? KhachHangId
        {
            get => _khachHangId;
            set => SetField(ref _khachHangId, value);
        }

        private Guid? _voucherId;
        public Guid? VoucherId
        {
            get => _voucherId;
            set => SetField(ref _voucherId, value);
        }

        private string? _phanLoai;
        public string? PhanLoai
        {
            get => _phanLoai;
            set => SetField(ref _phanLoai, value, nameof(SortOrder));
        }

        private string? _ghiChu;
        public string? GhiChu
        {
            get => _ghiChu;
            set => SetField(ref _ghiChu, value, nameof(SortOrder));
        }

        private string? _ghiChuShipper;
        public string? GhiChuShipper
        {
            get => _ghiChuShipper;
            set => SetField(ref _ghiChuShipper, value, nameof(SortOrder));
        }

        private int _stt;
        public int Stt
        {
            get => _stt;
            set => SetField(ref _stt, value, nameof(SortOrder));
        }

        private bool? _isBank;
        public bool? IsBank
        {
            get => _isBank;
            set => SetField(ref _isBank, value, nameof(SortOrder));
        }

        // ================== MONEY ==================

        private decimal _thanhTien;
        public decimal ThanhTien
        {
            get => _thanhTien;
            set => SetField(ref _thanhTien, value);
        }

        private decimal _conLai;
        public decimal ConLai
        {
            get => _conLai;
            set => SetField(ref _conLai, value, nameof(SortOrder));
        }
        private decimal _daThu;
        public decimal DaThu
        {
            get => _daThu;
            set => SetField(ref _daThu, value, nameof(SortOrder));
        }
        // ================== TIME ==================

        private DateTime? _ngayGio;
        public DateTime? NgayGio
        {
            get => _ngayGio;
            set => SetField(ref _ngayGio, value, nameof(WaitingTime));
        }

        private DateTime? _ngayShip;
        public DateTime? NgayShip
        {
            get => _ngayShip;
            set => SetField(ref _ngayShip, value, nameof(WaitingTime));
        }

        private DateTime? _ngayNo;
        public DateTime? NgayNo
        {
            get => _ngayNo;
            set => SetField(ref _ngayNo, value, nameof(SortOrder));
        }

        private DateTime? _ngayIn;
        public DateTime? NgayIn
        {
            get => _ngayIn;
            set => SetField(ref _ngayIn, value, nameof(SortOrder));
        }

        private DateTime? _lastModified;
        public DateTime? LastModified
        {
            get => _lastModified;
            set => SetField(ref _lastModified, value);
        }

        // ================== SHIPPER ==================

        private string? _nguoiShip;
        public string? NguoiShip
        {
            get => _nguoiShip;
            set => SetField(ref _nguoiShip, value,
                nameof(ChuaCoShipper),
                nameof(SortOrder));
        }

        public bool ChuaCoShipper => string.IsNullOrWhiteSpace(NguoiShip);

        // ================== SEARCH ==================

        public string TimKiem =>
        $"{TenKhachHangText ?? ""} " +
        $"{StringHelper.MyNormalizeText(TenKhachHangText ?? "")} " +
        $"{StringHelper.MyNormalizeText((TenKhachHangText ?? "").Replace(" ", ""))} " +
        $"{StringHelper.GetShortName(TenKhachHangText ?? "")} " +
        $"{GhiChu ?? ""} " +
        $"{StringHelper.MyNormalizeText(GhiChu ?? "")} " +
        $"{StringHelper.MyNormalizeText((GhiChu ?? "").Replace(" ", ""))} " +
        $"{StringHelper.GetShortName(GhiChu ?? "")} " +
        $"{StringHelper.MyNormalizeText(GhiChu ?? "")}";
        // ================== COMPUTED ==================

        public int SortOrder
        {
            get
            {
                if (ConLai <= 0 && PhanLoai == "Ship" && ChuaCoShipper)
                    return 1;

                if (ConLai <= 0)
                    return 6;

                if (NgayNo != null)
                    return 7;

                if (PhanLoai == "Ship" && ChuaCoShipper)
                    return 1;

                if (PhanLoai == "Ship" && !ChuaCoShipper)
                    return 5;

                if (PhanLoai == "Mv")
                    return 2;

                if (PhanLoai == "App")
                    return 3;

                return 4;
            }
        }

        public string WaitingTime
        {
            get
            {
                var baseTime = NgayShip ?? NgayGio;

                if (baseTime == null)
                    return "";

                var minutes = (int)(DateTime.Now - baseTime.Value).TotalMinutes;

                if (minutes < 1) return "Mới";

                if (minutes < 60)
                    return $"{minutes}p";

                var hours = minutes / 60;
                var mins = minutes % 60;

                return $"{hours}h {mins}p";
            }
        }

        // ================== TIMER SUPPORT ==================

        public void RefreshWaitingTime()
        {
            OnPropertyChanged(nameof(WaitingTime));
        }
    }
}