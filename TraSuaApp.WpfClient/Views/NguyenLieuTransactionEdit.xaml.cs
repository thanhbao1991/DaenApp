using System.Windows;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.AdminViews;

public partial class NguyenLieuTransactionEdit : Window
{
    private readonly NguyenLieuTransactionApi _api = new();
    public NguyenLieuTransactionDto Model { get; set; } = new();

    public NguyenLieuTransactionEdit(NguyenLieuTransactionDto? dto = null)
    {
        InitializeComponent();

        NguyenLieuComboBox.ItemsSource = AppProviders.NguyenLieuBanHangs.Items;
        LoaiComboBox.ItemsSource = Enum.GetValues(typeof(LoaiGiaoDichNguyenLieu));

        if (dto != null)
        {
            Model = dto;
            NguyenLieuComboBox.SelectedValue = dto.NguyenLieuId;
            LoaiComboBox.SelectedItem = dto.Loai;
            SoLuongTextBox.Text = dto.SoLuong.ToString();
            GhiChuTextBox.Text = dto.GhiChu;
        }
        else
        {
            Model.NgayGio = DateTime.Now;
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(SoLuongTextBox.Text, out var soLuong) || soLuong == 0)
        {
            ErrorTextBlock.Text = "Số lượng không hợp lệ.";
            return;
        }

        Model.NguyenLieuId = (Guid)NguyenLieuComboBox.SelectedValue;
        Model.Loai = (LoaiGiaoDichNguyenLieu)LoaiComboBox.SelectedItem!;
        Model.SoLuong = soLuong;
        Model.GhiChu = GhiChuTextBox.Text;

        var result = Model.Id == Guid.Empty
            ? await _api.CreateAsync(Model)
            : await _api.UpdateAsync(Model.Id, Model);

        if (!result.IsSuccess)
        {
            ErrorTextBlock.Text = result.Message;
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();
}
