using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.HoaDonViews
{
    public partial class ChiTieuHangNgayEditN : Window, INotifyPropertyChanged
    {
        private readonly IChiTieuHangNgayApi _api;
        private readonly ChiTieuHangNgayDto? _editingItem;
        private readonly bool _isEdit;
        /// <summary>
        /// 
        /// </summary>
        public ObservableCollection<ChiTieuRowVm> Items { get; } = new();

        public decimal TongTien => Items.Sum(x => x.ThanhTien);

        // ===== CREATE =====
        public ChiTieuHangNgayEditN()
        {
            InitializeComponent();
            DataContext = this;
            int i;
            _api = new ChiTieuHangNgayApi();
            NgayDatePicker.SelectedDate = DateTime.Today;

            NguyenLieuComboBox.NguyenLieuList = AppProviders.NguyenLieus.Items.ToList();

            NguyenLieuComboBox.NguyenLieuSelected += OnNguyenLieuSelected;

            Items.CollectionChanged += Items_CollectionChanged;
            NguyenLieuComboBox.Focus();
        }
        private void NhapTatCaNguyenLieu_Click(object sender, RoutedEventArgs e)
        {
            if (_isEdit) return;

            var existingIds = Items
                .Select(x => x.NguyenLieuId)
                .ToHashSet();

            foreach (var nl in AppProviders.NguyenLieus.Items)
            {
                // tránh thêm trùng
                if (existingIds.Contains(nl.Id))
                    continue;

                var vm = new ChiTieuRowVm
                {
                    NguyenLieuId = nl.Id,
                    Ten = nl.Ten,
                    SoLuong = 0,                // ✅ MẶC ĐỊNH 0
                    DonGia = nl.GiaNhap,
                    BillThang = BillThangCheckBox.IsChecked == true
                };

                Items.Add(vm);
            }
        }
        private void XoaDongSoLuong0_Click(object sender, RoutedEventArgs e)
        {
            if (_isEdit) return;

            var toRemove = Items
                .Where(x => x.SoLuong == 0)
                .ToList();

            foreach (var item in toRemove)
                Items.Remove(item);
        }

        // ===== EDIT =====
        public ChiTieuHangNgayEditN(ChiTieuHangNgayDto editItem) : this()
        {
            _editingItem = editItem;
            _isEdit = true;

            NgayDatePicker.SelectedDate = editItem.Ngay;
            NguyenLieuComboBox.IsEnabled = false;
            BillThangCheckBox.IsChecked = editItem.BillThang;

            var vm = new ChiTieuRowVm
            {
                Id = editItem.Id,
                NguyenLieuId = editItem.NguyenLieuId,
                Ten = editItem.Ten,
                SoLuong = editItem.SoLuong,
                DonGia = editItem.DonGia,
                GhiChu = editItem.GhiChu,
                BillThang = editItem.BillThang
            };

            HookRow(vm);
            Items.Add(vm);

            NotifyTongTien();
        }

        // ===== HANDLE COLLECTION =====
        private void Items_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ChiTieuRowVm vm in e.NewItems)
                    HookRow(vm);
            }

            NotifyTongTien();
        }

        private void HookRow(ChiTieuRowVm vm)
        {
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ChiTieuRowVm.ThanhTien))
                    NotifyTongTien();
            };
        }

        private void NotifyTongTien()
        {
            OnPropertyChanged(nameof(TongTien));
        }

        // ===== NGUYÊN LIỆU SELECT =====
        private void OnNguyenLieuSelected(NguyenLieuDto nl)
        {
            if (_isEdit) return;

            var vm = new ChiTieuRowVm
            {
                NguyenLieuId = nl.Id,
                Ten = nl.Ten,
                SoLuong = 1,
                DonGia = nl.GiaNhap,
                BillThang = BillThangCheckBox.IsChecked == true
            };

            Items.Add(vm);
        }

        // ===== REMOVE ROW =====
        private void RemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (_isEdit) return;

            if ((sender as FrameworkElement)?.Tag is ChiTieuRowVm vm)
                Items.Remove(vm);
        }

        // ===== BILL THÁNG =====
        private void BillThangChanged(object sender, RoutedEventArgs e)
        {
            var value = BillThangCheckBox.IsChecked == true;
            foreach (var i in Items)
                i.BillThang = value;
        }

        // ===== SAVE =====
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            if (!Items.Any())
            {
                ErrorTextBlock.Text = "Chưa có dòng chi tiêu.";
                return;
            }

            var row = Items[0];

            if (_isEdit)
            {
                var dto = new ChiTieuHangNgayDto
                {
                    Id = row.Id,
                    NguyenLieuId = _editingItem?.NguyenLieuId ?? row.NguyenLieuId,
                    Ten = string.IsNullOrWhiteSpace(row.Ten)
                        ? (_editingItem?.Ten ?? "")
                        : row.Ten,
                    SoLuong = row.SoLuong,
                    DonGia = row.DonGia,
                    ThanhTien = row.ThanhTien,
                    GhiChu = row.GhiChu,
                    BillThang = row.BillThang,

                    Ngay = _editingItem?.Ngay ?? (NgayDatePicker.SelectedDate ?? DateTime.Today),
                    NgayGio = _editingItem?.NgayGio ?? DateTime.Now,
                    LastModified = _editingItem?.LastModified ?? DateTime.Now,
                    CreatedAt = _editingItem?.CreatedAt ?? DateTime.Now
                };

                var res = await _api.UpdateAsync(row.Id, dto);
                if (!res.IsSuccess)
                {
                    ErrorTextBlock.Text = res.Message;
                    return;
                }
            }
            else
            {
                var ngay = NgayDatePicker.SelectedDate ?? DateTime.Today;
                var now = DateTime.Now;

                if (Items.Count == 1)
                {
                    var res = await _api.CreateAsync(row.ToDto(ngay, now));
                    if (!res.IsSuccess)
                    {
                        ErrorTextBlock.Text = res.Message;
                        return;
                    }
                }
                else
                {
                    var bulk = new ChiTieuHangNgayBulkCreateDto
                    {
                        Ngay = ngay,
                        NgayGio = now,
                        BillThang = BillThangCheckBox.IsChecked == true,
                        Items = Items.Select(x => x.ToBulkItem()).ToList()
                    };

                    var res = await _api.CreateBulkAsync(bulk);
                    if (!res.IsSuccess)
                    {
                        ErrorTextBlock.Text = res.Message;
                        return;
                    }
                }
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => Close();

        // ===== INotifyPropertyChanged =====
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private void ChiTieuGrid_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (ChiTieuGrid.SelectedItem is not ChiTieuRowVm vm)
                return;

            int index = Items.IndexOf(vm);

            // ===== ↑ CHUYỂN DÒNG LÊN =====
            if (e.Key == System.Windows.Input.Key.Up)
            {
                if (index > 0)
                {
                    ChiTieuGrid.SelectedItem = Items[index - 1];
                    ChiTieuGrid.ScrollIntoView(Items[index - 1]);
                }
                e.Handled = true;
                return;
            }

            // ===== ↓ CHUYỂN DÒNG XUỐNG =====
            if (e.Key == System.Windows.Input.Key.Down)
            {
                if (index < Items.Count - 1)
                {
                    ChiTieuGrid.SelectedItem = Items[index + 1];
                    ChiTieuGrid.ScrollIntoView(Items[index + 1]);
                }
                e.Handled = true;
                return;
            }

            // ===== DELETE → XOÁ =====
            if (!_isEdit && e.Key == System.Windows.Input.Key.Delete)
            {
                Items.Remove(vm);
                e.Handled = true;
                return;
            }

            // ===== + TĂNG SL =====
            if (e.Key == System.Windows.Input.Key.OemPlus || e.Key == System.Windows.Input.Key.Add || e.Key == System.Windows.Input.Key.Right)
            {
                vm.SoLuong += 1;
                e.Handled = true;
                return;
            }

            // ===== - GIẢM SL =====
            if (e.Key == System.Windows.Input.Key.OemMinus || e.Key == System.Windows.Input.Key.Subtract || e.Key == System.Windows.Input.Key.Left)
            {
                if (vm.SoLuong > 0)
                    vm.SoLuong -= 1;
                e.Handled = true;
                return;
            }
        }

    }

    // ======================================================
    // ================= ROW VIEW MODEL =====================
    // ======================================================
    public class ChiTieuRowVm : INotifyPropertyChanged
    {
        public Guid Id { get; set; }
        public Guid NguyenLieuId { get; set; }
        public string Ten { get; set; } = "";

        private decimal _soLuong;
        public decimal SoLuong
        {
            get => _soLuong;
            set
            {
                _soLuong = value;
                Notify(nameof(SoLuong));
                Notify(nameof(ThanhTien));
            }
        }

        private decimal _donGia;
        public decimal DonGia
        {
            get => _donGia;
            set
            {
                _donGia = value;
                Notify(nameof(DonGia));
                Notify(nameof(ThanhTien));
            }
        }

        public decimal ThanhTien => SoLuong * DonGia;

        public string GhiChu { get; set; } = "";
        public bool BillThang { get; set; }

        public ChiTieuHangNgayDto ToDto(DateTime ngay, DateTime gio) => new()
        {
            NguyenLieuId = NguyenLieuId,
            Ten = Ten,
            SoLuong = SoLuong,
            DonGia = DonGia,
            ThanhTien = ThanhTien,
            GhiChu = GhiChu,
            Ngay = ngay,
            NgayGio = gio,
            BillThang = BillThang
        };

        public ChiTieuHangNgayBulkItemDto ToBulkItem() => new()
        {
            NguyenLieuId = NguyenLieuId,
            SoLuong = SoLuong,
            DonGia = DonGia,
            ThanhTien = ThanhTien,
            GhiChu = GhiChu,
            BillThang = BillThang
        };

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Notify(string n)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}