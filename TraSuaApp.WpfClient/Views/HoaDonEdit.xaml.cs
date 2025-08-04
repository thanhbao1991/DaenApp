using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Controls;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class HoaDonEdit : Window
    {
        public HoaDonDto Model { get; set; } = new();
        private readonly IHoaDonApi _api;
        string _friendlyName = TuDien._tableFriendlyNames["HoaDon"];

        private List<SanPhamDto> _sanPhamList = new();
        private List<SanPhamBienTheDto> _bienTheList = new();
        private List<ToppingDto> _toppingList = new();
        private List<VoucherDto> _voucherList = new();
        private List<KhachHangDto> _khachHangsList = new();
        private readonly string[] _banList = new[]
{
    "Bàn 2", "Bàn 3", "Bàn 4", "Bàn 5", "Bàn 6",
    "Bàn 7", "Bàn 8", "Bàn 9", "Bàn 10", "Bàn 13",
    "Sân 1", "Sân 2"
};



        public HoaDonEdit(HoaDonDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _sanPhamList = AppProviders.SanPhams.Items.Where(x => !x.NgungBan).ToList();
            _bienTheList = _sanPhamList.SelectMany(x => x.BienThe).ToList();
            _toppingList = AppProviders.Toppings.Items.ToList();
            _voucherList = AppProviders.Vouchers.Items.ToList();
            _khachHangsList = AppProviders.KhachHangs.Items.ToList();

            KhachHangSearchBox.KhachHangList = _khachHangsList;
            KhachHangSearchBox.KhachHangSelected += kh =>
            {
                Model.KhachHangId = kh.Id;
                var diaChiList = kh.Addresses?.ToList() ?? new();
                DiaChiComboBox.ItemsSource = diaChiList;
                DiaChiComboBox.SelectedItem = diaChiList.FirstOrDefault(x => x.IsDefault) ?? diaChiList.LastOrDefault();
                var sdtList = kh.Phones?.ToList() ?? new();
                DienThoaiComboBox.ItemsSource = sdtList;
                DienThoaiComboBox.SelectedItem = sdtList.FirstOrDefault(x => x.IsDefault) ?? sdtList.LastOrDefault();
                SanPhamSearchBox.SearchTextBox.Focus();
            };
            KhachHangSearchBox.KhachHangCleared += () =>
            {
                Model.KhachHangId = null;
                DiaChiComboBox.ItemsSource = null;
                DienThoaiComboBox.ItemsSource = null;
            };

            SanPhamSearchBox.SanPhamList = _sanPhamList;
            SanPhamSearchBox.SanPhamSelected += kh =>
            {
                ResetSanPhamInputs();

                if (SanPhamSearchBox.SelectedSanPham is not SanPhamDto sp) return;

                var bienThes = _bienTheList.Where(x => x.SanPhamId == sp.Id).ToList();
                BienTheComboBox.ItemsSource = bienThes;

                var selectedBienThe = bienThes.OrderBy(x => x.GiaBan).FirstOrDefault();
                if (selectedBienThe != null)
                {
                    _isAddingFromSearch = true;
                    BienTheComboBox.SelectedValue = selectedBienThe.Id;
                    SoLuongTextBox.Text = "1";
                    CapNhatChiTietSanPham(1, true);
                    _isAddingFromSearch = false;

                    var addedItem = Model.ChiTietHoaDons.FirstOrDefault(x => x.SanPhamIdBienThe == selectedBienThe.Id);
                    if (addedItem != null)
                    {
                        ChiTietListBox.SelectedItem = addedItem;
                        ChiTietListBox.ScrollIntoView(addedItem);
                    }
                }
                LoadToppingPanel(sp.NhomSanPhamId);




            };
            SanPhamSearchBox.SanPhamCleared += () => ResetSanPhamInputs();

            VoucherComboBox.ItemsSource = _voucherList;
            TenBanComboBox.ItemsSource = _banList;

            if (Model.KhachHangId != null)
            {
                var kh = _khachHangsList.FirstOrDefault(k => k.Id == Model.KhachHangId);
                if (kh != null)
                    KhachHangSearchBox.SetSelectedKhachHang(kh);
            }

            if (Model.VoucherId != null)
                VoucherComboBox.SelectedValue = Model.VoucherId;

            _api = new HoaDonApi();

            if (dto != null)
            {
                Model = dto;
                DiaChiComboBox.Text = Model.DiaChiText;
                DienThoaiComboBox.Text = Model.SoDienThoaiText;
            }
            else
            {
                Model.TrangThai = "Chờ";
                Model.MaHoaDon = "HD" + DateTime.Now.Ticks.ToString()[^6..];
            }

            if (Model.IsDeleted)
            {
                SaveButton.Content = "Khôi phục";
            }
            if (Model.ChiTietHoaDons == null)
                Model.ChiTietHoaDons = new List<ChiTietHoaDonDto>();

            ChiTietListBox.ItemsSource = Model.ChiTietHoaDons;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            Model.TenBan = TenBanComboBox.Text;
            Model.KhachHangId = KhachHangSearchBox.SelectedKhachHang?.Id;
            // ✅ Gán số điện thoại và địa chỉ vào Model
            Model.SoDienThoaiText = DienThoaiComboBox.Text?.Trim();
            Model.DiaChiText = DiaChiComboBox.Text?.Trim();
            // ✅ Gửi tên khách hàng mới nếu người dùng nhập tên nhưng chưa chọn khách hàng có sẵn
            if (KhachHangSearchBox.SelectedKhachHang == null
                && !string.IsNullOrWhiteSpace(KhachHangSearchBox.SearchTextBox.Text))
            {
                Model.TenKhachHang = KhachHangSearchBox.SearchTextBox.Text.Trim();
            }
            Model.VoucherId = (Guid?)VoucherComboBox.SelectedValue;


            Model.ChiTietHoaDonVouchers = new List<ChiTietHoaDonVoucherDto>();

            if (VoucherComboBox.SelectedItem is VoucherDto voucher && voucher.Id != Guid.Empty)
            {
                Model.ChiTietHoaDonVouchers.Add(new ChiTietHoaDonVoucherDto
                {
                    VoucherId = voucher.Id,
                    GiaTriApDung = voucher.GiaTri
                });
            }


            if (string.IsNullOrWhiteSpace(Model.MaHoaDon))
            {
                ErrorTextBlock.Text = $"Mã hóa đơn không được để trống.";
                return;
            }

            if (TaiChoRadio.IsChecked == true && string.IsNullOrWhiteSpace(Model.TenBan))
            {
                ErrorTextBlock.Text = "Tên bàn không được để trống.";
                TenBanComboBox.IsDropDownOpen = true;
                return;
            }

            if (Model.ChiTietHoaDons.Count == 0)
            {
                ErrorTextBlock.Text = "Chưa có sản phẩm nào trong hóa đơn.";
                return;
            }

            // Xử lý thêm địa chỉ và SĐT mới (UI -> DTO)
            if (KhachHangSearchBox.SelectedKhachHang is KhachHangDto kh)
            {
                string diaChiText = DiaChiComboBox.Text?.Trim() ?? string.Empty;
                var diaChiSelected = DiaChiComboBox.SelectedItem as KhachHangAddressDto;
                if (!string.IsNullOrWhiteSpace(diaChiText))
                {
                    bool diaChiTrung = kh.Addresses.Any(a => a.DiaChi.Equals(diaChiText, StringComparison.OrdinalIgnoreCase));
                    if (!diaChiTrung)
                    {
                        var diaChiMoi = new KhachHangAddressDto
                        {
                            Id = Guid.NewGuid(),
                            DiaChi = diaChiText,
                            IsDefault = false
                        };
                        kh.Addresses.Add(diaChiMoi);
                        DiaChiComboBox.ItemsSource = null;
                        DiaChiComboBox.ItemsSource = kh.Addresses;
                        DiaChiComboBox.SelectedItem = diaChiMoi;
                    }
                }

                string sdtText = DienThoaiComboBox.Text?.Trim() ?? string.Empty;
                var sdtSelected = DienThoaiComboBox.SelectedItem as KhachHangPhoneDto;
                if (!string.IsNullOrWhiteSpace(sdtText))
                {
                    bool sdtTrung = kh.Phones.Any(p => p.SoDienThoai.Equals(sdtText, StringComparison.OrdinalIgnoreCase));
                    if (!sdtTrung)
                    {
                        var sdtMoi = new KhachHangPhoneDto
                        {
                            Id = Guid.NewGuid(),
                            SoDienThoai = sdtText,
                            IsDefault = false
                        };
                        kh.Phones.Add(sdtMoi);
                        DienThoaiComboBox.ItemsSource = null;
                        DienThoaiComboBox.ItemsSource = kh.Phones;
                        DienThoaiComboBox.SelectedItem = sdtMoi;
                    }
                }
            }

            Result<HoaDonDto> result;
            if (Model.Id == Guid.Empty)
                result = await _api.CreateAsync(Model);
            else if (Model.IsDeleted)
                result = await _api.RestoreAsync(Model.Id);
            else
                result = await _api.UpdateAsync(Model.Id, Model);

            if (!result.IsSuccess)
            {
                ErrorTextBlock.Text = result.Message;
                return;
            }

            DialogResult = true;
            Close();
        }



        private void ResetSanPhamInputs()
        {
            // Reset combobox biến thể
            BienTheComboBox.ItemsSource = null;
            BienTheComboBox.SelectedIndex = -1;

            // Reset số lượng
            SoLuongTextBox.Text = "0";

            // Bỏ chọn tất cả radio trong Tab Note
            foreach (var radio in this.FindVisualChildren<RadioButton>().Where(r => r.GroupName != "LoaiDon"))
            {
                radio.IsChecked = false;
            }

            // Reset topping về 0
            foreach (var row in ToppingPanel.Children.OfType<StackPanel>())
            {
                var txt = row.Children.OfType<TextBlock>().FirstOrDefault();
                if (txt != null) txt.Text = "0";
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseButton_Click(null!, null!);
                return;
            }

            if (e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement is Button) return;

                var request = new TraversalRequest(FocusNavigationDirection.Next);
                if (Keyboard.FocusedElement is UIElement element)
                {
                    element.MoveFocus(request);
                    e.Handled = true;
                }
            }
        }


        private bool _isAddingFromSearch = false;
        private void BienTheComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isAddingFromSearch) return; // ✅ Không tự thêm lại

            if (SanPhamSearchBox.SelectedSanPham is not SanPhamDto sanPham ||
                BienTheComboBox.SelectedItem is not SanPhamBienTheDto bienThe)
                return;

            // Nếu chưa có số lượng, mặc định = 1 và thêm sản phẩm
            if (!int.TryParse(SoLuongTextBox.Text, out int sl) || sl <= 0)
            {
                sl = 1;
                SoLuongTextBox.Text = "1";
                CapNhatChiTietSanPham(sl);
            }

            // Lấy dòng hiện tại trong list
            var existing = (!ChiTietListBox.SelectedItems.Contains(null))
           ? ChiTietListBox.SelectedItem as ChiTietHoaDonDto
           : null;

            if (existing == null)
            {
                existing = Model.ChiTietHoaDons.FirstOrDefault(x => x.SanPhamIdBienThe == bienThe.Id);
            }

            if (existing != null)
            {
                // ✅ Cập nhật lại thông tin biến thể và giá
                existing.SanPhamIdBienThe = bienThe.Id;
                existing.TenBienThe = bienThe.TenBienThe;
                existing.DonGia = bienThe.GiaBan;
            }

            // Refresh UI
            ChiTietListBox.Items.Refresh();
            CapNhatTongTien();

            SanPhamSearchBox.SearchTextBox.Focus();
            SanPhamSearchBox.SearchTextBox.SelectAll();

        }
        private void LoadToppingPanel(Guid? nhomSanPhamId)
        {
            var dsTopping = _toppingList
                .Where(t => t.NhomSanPhams.Contains(nhomSanPhamId ?? Guid.Empty))
                .ToList().OrderBy(x => x.Ten);

            ToppingPanel.Children.Clear();

            foreach (var topping in dsTopping)
            {
                var row = new StackPanel
                {
                    Orientation = Orientation.Horizontal,

                };

                var decreaseButton = new Button
                {
                    Content = "-",
                    Padding = new Thickness(8, 4, 8, 4),
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    BorderBrush = Brushes.White,
                    Background = Brushes.White,
                    Margin = new Thickness(0, 0, 4, 0),

                    Tag = topping.Id
                };

                var quantityText = new TextBlock
                {
                    Text = "0",
                    Padding = new Thickness(4),
                    Margin = new Thickness(0, 0, 4, 0),
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = topping
                };

                var label = new TextBlock
                {
                    Text = $"{topping.Ten}",
                    VerticalAlignment = VerticalAlignment.Center
                };
                label.MouseDown += (s, e) =>
                {
                    int current = int.Parse(quantityText.Text);
                    quantityText.Text = (++current).ToString();
                    CapNhatToppingChoSanPham();
                };

                decreaseButton.Click += (s, e) =>
                {
                    int current = int.Parse(quantityText.Text);
                    if (current > 0) current--;
                    quantityText.Text = current.ToString();
                    CapNhatToppingChoSanPham();
                };

                row.Children.Add(decreaseButton);
                row.Children.Add(quantityText);
                row.Children.Add(label);
                // row.Children.Add(increaseButton);

                ToppingPanel.Children.Add(row);
            }
        }

        private void CapNhatTongTien()
        {
            decimal tongTien = 0;

            // Tính tổng tiền
            foreach (var ct in Model.ChiTietHoaDons)
            {
                decimal tienTopping = ct.ToppingDtos?.Sum(t => t.Gia * t.SoLuong) ?? 0;
                tongTien += (ct.DonGia * ct.SoLuong) + tienTopping;
            }

            decimal giamGia = 0;

            // ✅ Lấy voucher hiện đang chọn (nếu có)
            if (VoucherComboBox.SelectedItem is VoucherDto voucher)
            {
                if (voucher.KieuGiam == "%")
                {
                    giamGia = tongTien * voucher.GiaTri / 100;
                }
                else
                {
                    giamGia = voucher.GiaTri;
                }
            }
            else if (Model.GiamGia > 0)
            {
                giamGia = Model.GiamGia;
            }

            if (giamGia > tongTien)
                giamGia = tongTien;

            // ✅ Cập nhật model trước khi lưu
            Model.TongTien = tongTien;
            Model.GiamGia = giamGia;
            Model.ThanhTien = tongTien - giamGia;

            TongTienTextBlock.Text = Model.TongTien.ToString("N0") + " đ";
            GiamGiaTextBlock.Text = Model.GiamGia.ToString("N0") + " đ";
            ThanhTienTextBlock.Text = Model.ThanhTien.ToString("N0") + " đ";
        }

        private void CapNhatToppingChoSanPham()
        {
            if (SanPhamSearchBox.SelectedSanPham is not SanPhamDto sanPham ||
                BienTheComboBox.SelectedItem is not SanPhamBienTheDto bienThe)
                return;

            var existing = ChiTietListBox.SelectedItem as ChiTietHoaDonDto;

            if (existing == null)
            {
                existing = Model.ChiTietHoaDons.FirstOrDefault(x => x.SanPhamIdBienThe == bienThe.Id);
            }

            if (existing == null) return;

            existing.ToppingDtos.Clear();

            // Xóa topping cũ trong danh sách chính (nếu có)
            foreach (var item in Model.ChiTietHoaDonToppings
            .Where(tp => tp.ChiTietHoaDonId == existing.Id).ToList())
            {
                Model.ChiTietHoaDonToppings.Remove(item);
            }
            foreach (var row in ToppingPanel.Children.OfType<StackPanel>())
            {
                var txt = row.Children.OfType<TextBlock>().FirstOrDefault(x => x.Tag is ToppingDto);
                if (txt?.Tag is ToppingDto topping && int.TryParse(txt.Text, out int sl) && sl > 0)
                {
                    existing.ToppingDtos.Add(new ToppingDto
                    {
                        Id = topping.Id,
                        Ten = topping.Ten,
                        Gia = topping.Gia,
                        SoLuong = sl
                    });

                    // ✅ Đồng bộ vào danh sách gửi API
                    Model.ChiTietHoaDonToppings.Add(new ChiTietHoaDonToppingDto
                    {
                        ChiTietHoaDonId = existing.Id,
                        ToppingId = topping.Id,
                        SoLuong = sl,
                        Gia = topping.Gia,
                        Ten = topping.Ten
                    });
                }
            }

            existing.ToppingText = existing.ToppingDtos.Any()
                ? string.Join(", ", existing.ToppingDtos.Select(t => $"{t.Ten} x{t.SoLuong}"))
                : "";

            ChiTietListBox.Items.Refresh();
            CapNhatTongTien();

            ChiTietListBox.SelectedItem = existing;
            ChiTietListBox.ScrollIntoView(existing);
            SanPhamSearchBox.SearchTextBox.Focus();
            SanPhamSearchBox.SearchTextBox.SelectAll();
        }

        private void XoaChiTietButton_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not ChiTietHoaDonDto ct) return;

            if (MessageBox.Show($"Xoá {ct.TenSanPham}?", "Xác nhận", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            Model.ChiTietHoaDons.Remove(ct);
            ChiTietListBox.Items.Refresh();
            CapNhatTongTien();
        }



        private void LoaiDonRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb)
            {
                NoiDungForm.IsEnabled = true;
                NoiDungForm.Opacity = 1;

                // Xử lý "Tại chỗ" => bật ComboBox bàn
                if (rb == TaiChoRadio)
                {
                    TenBanComboBox.Visibility = Visibility.Visible;
                    TenBanComboBox.IsDropDownOpen = true;

                }
                else
                {
                    TenBanComboBox.Visibility = Visibility.Collapsed;
                    TenBanComboBox.SelectedItem = null;
                    SanPhamSearchBox.SearchTextBox.Focus();
                }

                // Xử lý "App" => tự động chọn Voucher đầu tiên
                if (rb == AppRadio)
                {
                    if (VoucherComboBox.Items.Count > 0)
                    {
                        VoucherComboBox.SelectedIndex = VoucherComboBox.Items.Count - 1;
                    }
                }
            }

        }

        private void HuyVoucher_Click(object sender, RoutedEventArgs e)
        {
            VoucherComboBox.SelectedIndex = -1;
            Model.VoucherId = null;
            HuyVoucherButton.Visibility = Visibility.Collapsed;
            CapNhatTongTien();
        }

        private void VoucherComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Model.ChiTietHoaDonVouchers == null)
                Model.ChiTietHoaDonVouchers = new List<ChiTietHoaDonVoucherDto>();

            // Khi có voucher được chọn
            if (VoucherComboBox.SelectedItem is VoucherDto selectedVoucher)
            {
                Model.VoucherId = selectedVoucher.Id;

                // Xóa các voucher cũ để chỉ giữ 1 voucher hiện tại
                Model.ChiTietHoaDonVouchers.Clear();

                // Chỉ gửi dữ liệu cần thiết (VoucherId và GiaTriApDung)
                Model.ChiTietHoaDonVouchers.Add(new ChiTietHoaDonVoucherDto
                {
                    VoucherId = selectedVoucher.Id,
                    GiaTriApDung = selectedVoucher.GiaTri // nếu có, có thể tùy chỉnh
                });

                HuyVoucherButton.Visibility = Visibility.Visible;
            }
            else
            {
                // Khi bỏ chọn voucher
                Model.VoucherId = null;

                if (Model.ChiTietHoaDonVouchers != null)
                    Model.ChiTietHoaDonVouchers.Clear();

                HuyVoucherButton.Visibility = Visibility.Collapsed;
            }

            CapNhatTongTien();
        }

        private void TangButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(SoLuongTextBox.Text, out int sl)) sl = 0;
            sl++;
            SoLuongTextBox.Text = sl.ToString();
            CapNhatChiTietSanPham(sl);
        }

        private void GiamButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(SoLuongTextBox.Text, out int sl)) sl = 0;
            sl = Math.Max(0, sl - 1);
            SoLuongTextBox.Text = sl.ToString();
            CapNhatChiTietSanPham(sl);
        }
        private void CapNhatChiTietSanPham(int soLuong, bool forceNewLine = false)
        {
            if (SanPhamSearchBox.SelectedSanPham is not SanPhamDto sanPham ||
                BienTheComboBox.SelectedItem is not SanPhamBienTheDto bienThe)
                return;

            string currentNote = string.Join(", ", this.FindVisualChildren<RadioButton>()
                .Where(r => r.IsChecked == true && r.GroupName != "LoaiDon")
                .Select(r => r.Content.ToString()));

            ChiTietHoaDonDto? existing = null;

            // 🟟 Chỉ dùng dòng đang chọn nếu KHÔNG ép thêm mới
            if (!forceNewLine)
            {
                existing = ChiTietListBox.SelectedItem as ChiTietHoaDonDto
                           ?? Model.ChiTietHoaDons
                                .FirstOrDefault(x => x.SanPhamIdBienThe == bienThe.Id);
            }

            if (soLuong == 0)
            {
                if (existing != null)
                {
                    Model.ChiTietHoaDons.Remove(existing);
                    ChiTietListBox.Items.Refresh();
                    CapNhatTongTien();
                }
            }
            else
            {
                if (existing == null)
                {
                    // ✅ Luôn tạo dòng mới nếu forceNewLine = true hoặc không tìm thấy dòng phù hợp
                    existing = new ChiTietHoaDonDto
                    {
                        SanPhamIdBienThe = bienThe.Id,
                        TenSanPham = sanPham.Ten,
                        TenBienThe = bienThe.TenBienThe,
                        DonGia = bienThe.GiaBan,
                        SoLuong = soLuong,
                        BienTheList = _bienTheList.Where(bt => bt.SanPhamId == sanPham.Id).ToList(),
                        ToppingDtos = new List<ToppingDto>(),
                        NoteText = $"{currentNote}"
                    };
                    Model.ChiTietHoaDons.Add(existing);
                }
                else
                {
                    // ✅ Cập nhật số lượng dòng đang chọn
                    existing.SoLuong = soLuong;
                }

                // Đánh lại STT
                int stt = 1;
                foreach (var ct in Model.ChiTietHoaDons)
                {
                    ct.Stt = stt++;
                }

                ChiTietListBox.Items.Refresh();
                CapNhatTongTien();

                ChiTietListBox.SelectedItem = existing;
                ChiTietListBox.ScrollIntoView(existing);
                SanPhamSearchBox.SearchTextBox.Focus();
                SanPhamSearchBox.SearchTextBox.SelectAll();
            }

        }

        private void RadioButton_PreviewMouseLeftButtonDown_Common(object sender, MouseButtonEventArgs e)
        {
            if (sender is RadioButton rb && rb.IsChecked == true)
            {
                rb.IsChecked = false;
                e.Handled = true;
            }
        }
        private void NoteRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_isLoadingNote) return;
            CapNhatNoteChoSanPham();
        }
        private void TenBanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SanPhamSearchBox.SearchTextBox.Focus();

        }
        private bool _isLoadingNote = false;
        private void ChiTietListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChiTietListBox.SelectedItem is not ChiTietHoaDonDto selected)
                return;

            // ✅ Giữ lại tab hiện tại
            int currentTab = NoteToppingTabControl.SelectedIndex;

            _isLoadingNote = true;
            NoteToppingTabControl.UpdateLayout();

            // Bỏ chọn tất cả radio trước
            foreach (var radio in this.FindVisualChildren<RadioButton>().Where(r => r.GroupName != "LoaiDon"))
                radio.IsChecked = false;

            _isLoadingNote = false;

            // ✅ Chọn sản phẩm tương ứng
            var sanPham = _sanPhamList.FirstOrDefault(sp => sp.BienThe.Any(bt => bt.Id == selected.SanPhamIdBienThe));
            if (sanPham != null)
            {
                SanPhamSearchBox.SuppressPopup = true;
                SanPhamSearchBox.SetSelectedSanPham(sanPham);
                SanPhamSearchBox.SuppressPopup = false;

                // ✅ Load topping cho sản phẩm
                LoadToppingPanel(sanPham.NhomSanPhamId);
            }

            // ✅ Cập nhật số lượng topping
            foreach (var row in ToppingPanel.Children.OfType<StackPanel>())
            {
                var txt = row.Children.OfType<TextBlock>().FirstOrDefault(x => x.Tag is ToppingDto);
                if (txt?.Tag is ToppingDto topping)
                {
                    var match = selected.ToppingDtos.FirstOrDefault(t => t.Id == topping.Id);
                    txt.Text = match != null ? match.SoLuong.ToString() : "0";
                }
            }

            // ✅ Cập nhật note
            var allNotes = selected.NoteText?.Split('#')
                .Select(x => x.Trim())
                .ToList() ?? new List<string>();

            // Tick các radio tương ứng
            foreach (var radio in this.FindVisualChildren<RadioButton>().Where(r => r.GroupName != "LoaiDon"))
            {
                radio.IsChecked = allNotes?.Contains(radio.Content?.ToString() ?? "") ?? false;
            }

            // Hiển thị note tự do (không nằm trong radio)
            var predefinedNotes = this.FindVisualChildren<RadioButton>()
                .Select(r => r.Content.ToString())
                .ToHashSet();

            var freeNotes = allNotes?
      .Where(n => !predefinedNotes.Contains(n))
      ?? Enumerable.Empty<string>();
            NoteTuDoTextBox.Text = string.Join(" # ", freeNotes);

            // ✅ Cập nhật số lượng
            SoLuongTextBox.Text = selected.SoLuong.ToString();

            // ✅ Load biến thể
            BienTheComboBox.ItemsSource = selected.BienTheList;
            BienTheComboBox.SelectedValue = selected.SanPhamIdBienThe;

            // ✅ Khôi phục tab trước đó
            NoteToppingTabControl.SelectedIndex = currentTab;
        }
        private void ThemDongButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(SoLuongTextBox.Text, out int sl) || sl <= 0)
            {
                MessageBox.Show("Vui lòng nhập số lượng > 0 để thêm dòng mới.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ✅ Ép buộc tạo dòng mới
            CapNhatChiTietSanPham(sl, true);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int oldIndex = NoteToppingTabControl.SelectedIndex;
            NoteToppingTabControl.SelectedIndex = 0;
            NoteToppingTabControl.UpdateLayout();
            NoteToppingTabControl.SelectedIndex = oldIndex;
        }
        private void NoteTuDoTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadingNote) return;
            CapNhatNoteChoSanPham();
        }
        private void CapNhatNoteChoSanPham()
        {
            if (ChiTietListBox.SelectedItem is ChiTietHoaDonDto selected)
            {
                // Lấy note từ radio
                var selectedNotes = this.FindVisualChildren<RadioButton>()
                    .Where(r => r.IsChecked == true && r.GroupName != "LoaiDon")
                    .Select(r => r.Content.ToString())
                    .ToList();

                // Lấy note tự do
                var noteTuDo = NoteTuDoTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(noteTuDo))
                    selectedNotes.Add(noteTuDo);

                // Ghép bằng dấu #
                selected.NoteText = selectedNotes.Any()
                    ? string.Join(" # ", selectedNotes)
                    : "";

                ChiTietListBox.Items.Refresh();
            }
        }
    }
}

