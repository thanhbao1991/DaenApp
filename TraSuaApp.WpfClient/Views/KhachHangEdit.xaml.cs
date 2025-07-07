using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views;

public partial class KhachHangEdit : Window
{
    private readonly WpfErrorHandler _errorHandler;
    private readonly bool _isEdit;
    private KhachHangDto _khachHang = new();

    public bool IsSaved { get; private set; }

    public KhachHangEdit(KhachHangDto? khachHang = null)
    {
        InitializeComponent();
        _errorHandler = new WpfErrorHandler(ErrorTextBlock);

        _isEdit = khachHang != null;
        _khachHang = khachHang ?? new KhachHangDto();

        this.Loaded += (_, _) => LoadForm();
    }

    private void LoadForm()
    {
        TenTextBox.Text = _khachHang.Ten;
        PhoneTextBox.Text = _khachHang.DefaultPhone?.SoDienThoai ?? "";
        AddressTextBox.Text = _khachHang.DefaultAddress?.DiaChi ?? "";
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        SaveButton.IsEnabled = false;
        Mouse.OverrideCursor = Cursors.Wait;

        try
        {
            _errorHandler.Clear();

            var ten = TenTextBox.Text.Trim();
            var phone = PhoneTextBox.Text.Trim();
            var address = AddressTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(ten))
                throw new Exception("Tên không được để trống.");

            if (string.IsNullOrWhiteSpace(phone))
                throw new Exception("Số điện thoại không được để trống.");

            if (_khachHang.Phones == null) _khachHang.Phones = new();
            if (_khachHang.Addresses == null) _khachHang.Addresses = new();

            var id = _khachHang.Id == Guid.Empty ? Guid.NewGuid() : _khachHang.Id;
            _khachHang.Id = id;
            _khachHang.Ten = ten;

            _khachHang.Phones = new List<KhachHangPhoneDto>
            {
                new KhachHangPhoneDto
                {
                    Id = Guid.NewGuid(),
                    IdKhachHang = id,
                    SoDienThoai = phone,
                    IsDefault = true
                }
            };

            _khachHang.Addresses = new List<KhachHangAddressDto>
            {
                new KhachHangAddressDto
                {
                    Id = Guid.NewGuid(),
                    IdKhachHang = id,
                    DiaChi = address,
                    IsDefault = true
                }
            };

            HttpResponseMessage response = _isEdit
                ? await ApiClient.PutAsync($"/api/khachhang/{id}", _khachHang)
                : await ApiClient.PostAsync("/api/khachhang", _khachHang);

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
            _errorHandler.Handle(ex, "Lưu khách hàng");
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
}