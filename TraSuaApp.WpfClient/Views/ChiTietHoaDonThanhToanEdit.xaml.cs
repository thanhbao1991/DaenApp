//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Input;
//using System.Windows.Media;
//using TraSuaApp.Shared.Constants;
//using TraSuaApp.Shared.Dtos;
//using TraSuaApp.Shared.Helpers;
//using TraSuaApp.WpfClient.Apis;
//using TraSuaApp.WpfClient.Services;

//namespace TraSuaApp.WpfClient.HoaDonViews;

//public partial class ChiTietHoaDonThanhToanEdit : Window
//{
//    public ChiTietHoaDonThanhToanDto Model { get; set; } = new();

//    public bool QuickSubmit { get; set; }

//    private readonly IChiTietHoaDonThanhToanApi _api = new ChiTietHoaDonThanhToanApi();

//    private readonly List<PhuongThucThanhToanDto> _phuongThucThanhToanList;

//    private const string FriendlyName = "Chi tiết hóa đơn thanh toán";

//    public ChiTietHoaDonThanhToanEdit(ChiTietHoaDonThanhToanDto? dto = null)
//    {
//        InitializeComponent();

//        Title = FriendlyName;
//        TieuDeTextBlock.Text = FriendlyName;

//        KeyDown += Window_KeyDown;
//        Loaded += Window_Loaded;

//        _phuongThucThanhToanList =
//            AppProviders.PhuongThucThanhToans.Items
//            .Where(x => x.DangSuDung)
//            .ToList();

//        PhuongThucThanhToanComboBox.ItemsSource = _phuongThucThanhToanList;

//        InitModel(dto);
//    }

//    private void InitModel(ChiTietHoaDonThanhToanDto? dto)
//    {
//        if (dto == null)
//        {
//            TenTextBox.Focus();
//            return;
//        }

//        Model = dto;

//        TenTextBox.Text = dto.Ten;
//        NgayTextBox.Text = dto.Ngay.ToString("dd-MM-yyyy");
//        GioTextBox.Text = dto.NgayGio.ToString("HH:mm:ss");
//        LoaiThanhToanTextBox.Text = dto.LoaiThanhToan;

//        SoTienTextBox.Value = dto.SoTien;
//        SoTienTextBox.Focus();

//        PhuongThucThanhToanComboBox.SelectedValue = dto.PhuongThucThanhToanId;

//        if (Model.IsDeleted)
//        {
//            TenTextBox.IsEnabled = false;
//            SoTienTextBox.IsEnabled = false;
//            PhuongThucThanhToanComboBox.IsEnabled = false;

//            SaveButton.Content = "Khôi phục";
//        }
//    }

//    private async Task<bool> SaveAsync()
//    {
//        ErrorTextBlock.Text = "";

//        if (!decimal.TryParse(SoTienTextBox.Text.Replace(",", ""), out var tien))
//        {
//            ErrorTextBlock.Text = "Số tiền không hợp lệ.";
//            SoTienTextBox.Focus();
//            return false;
//        }

//        Model.SoTien = tien;

//        if (string.IsNullOrWhiteSpace(Model.LoaiThanhToan))
//        {
//            ErrorTextBlock.Text = "Vui lòng chọn loại thanh toán.";
//            return false;
//        }

//        if (PhuongThucThanhToanComboBox.SelectedItem is not PhuongThucThanhToanDto pt)
//        {
//            ErrorTextBlock.Text = "Vui lòng chọn phương thức thanh toán.";
//            PhuongThucThanhToanComboBox.Focus();
//            return false;
//        }

//        Model.PhuongThucThanhToanId = pt.Id;
//        Model.TenPhuongThucThanhToan = pt.Ten;

//        Result<ChiTietHoaDonThanhToanDto> result;

//        if (Model.Id == Guid.Empty)
//            result = await _api.CreateAsync(Model);
//        else if (Model.IsDeleted)
//            result = await _api.RestoreAsync(Model.Id);
//        else
//            result = await _api.UpdateAsync(Model.Id, Model);

//        if (!result.IsSuccess)
//        {
//            ErrorTextBlock.Text = result.Message;
//            return false;
//        }

//        return true;
//    }

//    private async void SaveButton_Click(object sender, RoutedEventArgs e)
//    {
//        if (await SaveAsync())
//        {
//            DialogResult = true;
//            Close();
//        }
//    }

//    private void CloseButton_Click(object sender, RoutedEventArgs e)
//        => Close();

//    private async void Window_Loaded(object sender, RoutedEventArgs e)
//    {
//        if (!QuickSubmit) return;

//        if (await SaveAsync())
//        {
//            DialogResult = true;
//            Close();
//        }
//    }

//    private async void Window_KeyDown(object sender, KeyEventArgs e)
//    {
//        if (e.Key == Key.Escape)
//        {
//            Close();
//            return;
//        }

//        if (e.Key == Key.Enter)
//        {
//            if (await SaveAsync())
//            {
//                DialogResult = true;
//                Close();
//            }
//        }
//    }

//    private void PhuongThucThanhToanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
//    {
//        if (PhuongThucThanhToanComboBox.SelectedValue is not Guid id)
//            return;

//        if (id == AppConstants.TienMatId)
//        {
//            ApplyTheme("SuccessBrush", "Tiền mặt");
//        }
//        else if (id == AppConstants.ChuyenKhoanId)
//        {
//            ApplyTheme("PrimaryBrush", "Chuyển khoản");
//        }
//    }

//    private void ApplyTheme(string brushKey, string buttonText)
//    {
//        var brush = (Brush)Application.Current.Resources[brushKey];

//        Background = MakeBrush(brush, 0.8);

//        SaveButton.Content = buttonText;
//        SaveButton.Foreground = Brushes.White;
//        SaveButton.Background = Background;
//    }

//    private static SolidColorBrush MakeBrush(Brush brush, double opacity)
//    {
//        if (brush is SolidColorBrush solid)
//        {
//            return new SolidColorBrush(solid.Color)
//            {
//                Opacity = opacity
//            };
//        }

//        return new SolidColorBrush(Colors.Transparent)
//        {
//            Opacity = opacity
//        };
//    }
//}


using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TraSuaApp.Shared.Constants;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.WpfClient.HoaDonViews;

public partial class ChiTietHoaDonThanhToanEdit : Window
{
    public ChiTietHoaDonThanhToanDto Model { get; set; } = new();

    public bool QuickSubmit { get; set; }

    private readonly List<PhuongThucThanhToanDto> _phuongThucThanhToanList;

    private const string FriendlyName = "Chi tiết hóa đơn thanh toán";

    public ChiTietHoaDonThanhToanEdit(ChiTietHoaDonThanhToanDto? dto = null)
    {
        InitializeComponent();

        Title = FriendlyName;
        TieuDeTextBlock.Text = FriendlyName;

        KeyDown += Window_KeyDown;
        Loaded += Window_Loaded;

        _phuongThucThanhToanList =
            AppProviders.PhuongThucThanhToans.Items
            .Where(x => x.DangSuDung)
            .ToList();

        PhuongThucThanhToanComboBox.ItemsSource = _phuongThucThanhToanList;

        InitModel(dto);
    }

    private void InitModel(ChiTietHoaDonThanhToanDto? dto)
    {
        if (dto == null)
        {
            TenTextBox.Focus();
            return;
        }

        Model = dto;

        TenTextBox.Text = dto.Ten;
        NgayTextBox.Text = dto.Ngay.ToString("dd-MM-yyyy");
        GioTextBox.Text = dto.NgayGio.ToString("HH:mm:ss");
        LoaiThanhToanTextBox.Text = dto.LoaiThanhToan;

        SoTienTextBox.Value = dto.SoTien;
        SoTienTextBox.Focus();

        PhuongThucThanhToanComboBox.SelectedValue = dto.PhuongThucThanhToanId;

        if (Model.IsDeleted)
        {
            TenTextBox.IsEnabled = false;
            SoTienTextBox.IsEnabled = false;
            PhuongThucThanhToanComboBox.IsEnabled = false;

            SaveButton.Content = "Khôi phục";
        }
    }

    // ==============================
    // ✅ VALIDATE ONLY (NO API)
    // ==============================
    private bool ValidateAndAssign()
    {
        ErrorTextBlock.Text = "";

        if (!decimal.TryParse(SoTienTextBox.Text.Replace(",", ""), out var tien))
        {
            ErrorTextBlock.Text = "Số tiền không hợp lệ.";
            SoTienTextBox.Focus();
            return false;
        }

        if (tien <= 0)
        {
            ErrorTextBlock.Text = "Số tiền phải lớn hơn 0.";
            SoTienTextBox.Focus();
            return false;
        }

        if (PhuongThucThanhToanComboBox.SelectedItem is not PhuongThucThanhToanDto pt)
        {
            ErrorTextBlock.Text = "Vui lòng chọn phương thức thanh toán.";
            PhuongThucThanhToanComboBox.Focus();
            return false;
        }

        // assign lại model
        Model.SoTien = tien;
        Model.PhuongThucThanhToanId = pt.Id;
        Model.TenPhuongThucThanhToan = pt.Ten;

        return true;
    }

    // ==============================
    // ✅ SAVE BUTTON (RETURN DATA)
    // ==============================
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateAndAssign())
            return;

        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    // ==============================
    // ⚡ QUICK SUBMIT (KHÔNG CALL API)
    // ==============================
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!QuickSubmit)
            return;

        if (ValidateAndAssign())
        {
            DialogResult = true;
            Close();
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            return;
        }

        if (e.Key == Key.Enter)
        {
            if (ValidateAndAssign())
            {
                DialogResult = true;
                Close();
            }
        }
    }

    private void PhuongThucThanhToanComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PhuongThucThanhToanComboBox.SelectedValue is not Guid id)
            return;

        if (id == AppConstants.TienMatId)
        {
            ApplyTheme("SuccessBrush", "Tiền mặt");
        }
        else if (id == AppConstants.ChuyenKhoanId)
        {
            ApplyTheme("PrimaryBrush", "Chuyển khoản");
        }
    }

    private void ApplyTheme(string brushKey, string buttonText)
    {
        var brush = (Brush)Application.Current.Resources[brushKey];

        Background = MakeBrush(brush, 0.8);

        SaveButton.Content = buttonText;
        SaveButton.Foreground = Brushes.White;
        SaveButton.Background = Background;
    }

    private static SolidColorBrush MakeBrush(Brush brush, double opacity)
    {
        if (brush is SolidColorBrush solid)
        {
            return new SolidColorBrush(solid.Color)
            {
                Opacity = opacity
            };
        }

        return new SolidColorBrush(Colors.Transparent)
        {
            Opacity = opacity
        };
    }
}