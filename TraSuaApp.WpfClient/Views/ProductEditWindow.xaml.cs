using System.Collections.ObjectModel;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class ProductEditWindow : Window
    {
        private readonly ErrorHandler _errorHandler = new WpfErrorHandler();
        private readonly bool _isEdit;
        private SanPhamDto _sanPham = new();
        private SanPhamBienTheDto? _bienTheDangChon = null;

        public bool IsSaved { get; private set; }

        public ProductEditWindow(SanPhamDto? sanPham = null)
        {
            InitializeComponent();
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
                MoTaTextBox.Text = _sanPham.MoTa;
                DaBanTextBlock.Text = (_sanPham.DaBan ?? 0).ToString("N0");
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
                ErrorTextBlock.Text = "";
                _sanPham.Ten = TenTextBox.Text.Trim();
                _sanPham.VietTat = VietTatTextBox.Text.Trim();
                _sanPham.MoTa = MoTaTextBox.Text.Trim();

                if (string.IsNullOrWhiteSpace(_sanPham.Ten))
                {
                    ErrorTextBlock.Text = "Tên sản phẩm không được để trống.";
                    return;
                }

                // Gán lại IdSanPham cho các biến thể
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
                    ErrorTextBlock.Text = $"Lỗi {(int)response.StatusCode}: {msg}";
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "SaveButton_Click");
            }
            finally
            {
                SaveButton.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

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
                ErrorTextBlock.Text = "";

                var ten = TenBienTheTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(ten))
                {
                    ErrorTextBlock.Text = "Tên biến thể không được để trống.";
                    return;
                }

                if (!decimal.TryParse(Regex.Replace(GiaBanTextBox.Text, @"[^\d]", ""), out var gia))
                {
                    ErrorTextBlock.Text = "Giá bán không hợp lệ.";
                    return;
                }

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
                _errorHandler.Handle(ex, "ThemBienTheButton_Click");
            }
        }

        private void XoaBienTheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button btn && btn.Tag is SanPhamBienTheDto bt)
                {
                    var bienThes = (ObservableCollection<SanPhamBienTheDto>)BienTheListBox.ItemsSource!;
                    bienThes.Remove(bt);
                    _bienTheDangChon = null;
                    TenBienTheTextBox.Text = "";
                    GiaBanTextBox.Text = "";
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "XoaBienTheButton_Click");
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
                if (_bienTheDangChon == null)
                {
                    ErrorTextBlock.Text = "Chưa chọn biến thể để sửa.";
                    return;
                }

                var ten = TenBienTheTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(ten))
                {
                    ErrorTextBlock.Text = "Tên biến thể không được để trống.";
                    return;
                }

                if (!decimal.TryParse(Regex.Replace(GiaBanTextBox.Text, @"[^\d]", ""), out var gia))
                {
                    ErrorTextBlock.Text = "Giá bán không hợp lệ.";
                    return;
                }

                _bienTheDangChon.TenBienThe = ten;
                _bienTheDangChon.GiaBan = gia;
                BienTheListBox.Items.Refresh();

                TenBienTheTextBox.Text = "";
                GiaBanTextBox.Text = "";
                _bienTheDangChon = null;
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "SuaBienTheButton_Click");
            }
        }
    }
}