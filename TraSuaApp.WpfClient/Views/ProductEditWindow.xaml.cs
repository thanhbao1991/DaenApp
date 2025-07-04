using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class ProductEditWindow : Window
    {
        private readonly WpfErrorHandler _errorHandler;
        private readonly bool _isEdit;
        private SanPhamDto _sanPham = new();
        private SanPhamBienTheDto? _bienTheDangChon = null;

        public bool IsSaved { get; private set; }

        public ProductEditWindow(SanPhamDto? sanPham = null)
        {
            InitializeComponent();
            _errorHandler = new WpfErrorHandler(ErrorTextBlock);

            _isEdit = sanPham != null;
            _sanPham = sanPham ?? new SanPhamDto();

            LoadForm();
            this.KeyDown += Window_KeyDown;
        }

        private void LoadForm()
        {
            try
            {
                TenTextBox.Text = _sanPham.Ten;
                VietTatTextBox.Text = _sanPham.VietTat;
                MoTaTextBox.Text = _sanPham.DinhLuong;
                NgungBanCheckBox.IsChecked = _sanPham.NgungBan;
                TichDiemCheckBox.IsChecked = _sanPham.TichDiem;


                //DaBanTextBlock.Text = (_sanPham.DaBan ?? 0).ToString("N0");

                BienTheListBox.ItemsSource = new ObservableCollection<SanPhamBienTheDto>(_sanPham.BienThe ?? []);
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "LoadForm");
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                _errorHandler.Clear();

                _sanPham.Ten = TenTextBox.Text.Trim();
                _sanPham.VietTat = VietTatTextBox.Text.Trim();
                _sanPham.DinhLuong = MoTaTextBox.Text.Trim();
                _sanPham.NgungBan = NgungBanCheckBox.IsChecked == true ? true : false;
                _sanPham.TichDiem = TichDiemCheckBox.IsChecked == true ? true : false;



                if (string.IsNullOrWhiteSpace(_sanPham.Ten))
                    throw new Exception("Tên sản phẩm không được để trống.");

                var bienThes = (BienTheListBox.ItemsSource as ObservableCollection<SanPhamBienTheDto>) ?? new();
                foreach (var bt in bienThes)
                    bt.IdSanPham = _sanPham.Id;

                _sanPham.BienThe = bienThes.ToList();

                HttpResponseMessage response = _isEdit
                    ? await ApiClient.PutAsync($"/api/sanpham/{_sanPham.Id}", _sanPham)
                    : await ApiClient.PostAsync("/api/sanpham", _sanPham);

                if (response.IsSuccessStatusCode)
                {
                    IsSaved = true;
                    DialogResult = true;
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API lỗi {(int)response.StatusCode}: {msg}");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Lưu sản phẩm");
            }
            finally
            {
                SaveButton.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SaveButton_Click(SaveButton, new RoutedEventArgs());
            else if (e.Key == Key.Escape)
                DialogResult = false;
        }

        private void GiaBanTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"\d");
        }

        private void GiaBanTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                var text = Regex.Replace(tb.Text, @"[^\d]", "");
                if (decimal.TryParse(text, out decimal value))
                {
                    tb.Text = value.ToString("N0", CultureInfo.InvariantCulture);
                    tb.CaretIndex = tb.Text.Length;
                }
            }
        }

        private void ThemBienTheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _errorHandler.Clear();

                var ten = TenBienTheTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(ten))
                    throw new Exception("Tên biến thể không được để trống.");

                if (!decimal.TryParse(Regex.Replace(GiaBanTextBox.Text, @"[^\d]", ""), out var gia))
                    throw new Exception("Giá bán không hợp lệ.");

                var bienThes = (ObservableCollection<SanPhamBienTheDto>)BienTheListBox.ItemsSource!;
                bienThes.Add(new SanPhamBienTheDto
                {
                    TenBienThe = ten,
                    GiaBan = gia,
                    IdSanPham = _sanPham.Id
                });

                TenBienTheTextBox.Text = "";
                GiaBanTextBox.Text = "";
                _bienTheDangChon = null;
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Thêm biến thể");
            }
        }

        private void XoaBienTheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (BienTheListBox.SelectedItem is SanPhamBienTheDto bt)
                {
                    var bienThes = (ObservableCollection<SanPhamBienTheDto>)BienTheListBox.ItemsSource!;
                    bienThes.Remove(bt);

                    _bienTheDangChon = null;
                    TenBienTheTextBox.Text = "";
                    GiaBanTextBox.Text = "";
                }
                else
                {
                    MessageBox.Show("Vui lòng chọn một biến thể để xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Xoá biến thể");
            }
        }

        private void BienTheListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BienTheListBox.SelectedItem is SanPhamBienTheDto bt)
            {
                _bienTheDangChon = bt;
                TenBienTheTextBox.Text = bt.TenBienThe;
                GiaBanTextBox.Text = bt.GiaBan.ToString("N0");
            }
        }

        private void SuaBienTheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _errorHandler.Clear();

                if (_bienTheDangChon == null)
                    throw new Exception("Chưa chọn biến thể để sửa.");

                var ten = TenBienTheTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(ten))
                    throw new Exception("Tên biến thể không được để trống.");

                if (!decimal.TryParse(Regex.Replace(GiaBanTextBox.Text, @"[^\d]", ""), out var gia))
                    throw new Exception("Giá bán không hợp lệ.");

                _bienTheDangChon.TenBienThe = ten;
                _bienTheDangChon.GiaBan = gia;

                BienTheListBox.Items.Refresh();

                TenBienTheTextBox.Text = "";
                GiaBanTextBox.Text = "";
                _bienTheDangChon = null;
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Sửa biến thể");
            }
        }
    }
}