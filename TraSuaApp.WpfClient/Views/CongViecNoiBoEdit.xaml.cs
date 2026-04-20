using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Config;
using TraSuaApp.WpfClient.Models;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.SettingsViews
{
    public partial class CongViecNoiBoEdit : Window
    {
        public CongViecNoiBoDto Model { get; set; } = new();

        private readonly string _friendlyName = TuDien._tableFriendlyNames["CongViecNoiBo"];
        private List<NhomSanPhamCheckItem> _bindingList = new();

        public CongViecNoiBoEdit(CongViecNoiBoDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;

            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            if (dto != null)
            {
                Model = dto;
                TenTextBox.Text = dto.Ten;
                XNgayCanhBaoTextBox.Value = dto.XNgayCanhBao ?? 0;
            }
            else
            {
                TenTextBox.Focus();
            }


        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            var api = Apis.CongViecNoiBo;

            Model.Ten = TenTextBox.Text.Trim();
            Model.XNgayCanhBao = (int)XNgayCanhBaoTextBox.Value;
            Model.DaHoanThanh = false;
            Model.NgayGio = DateTime.Now;

            if (string.IsNullOrWhiteSpace(Model.Ten))
            {
                ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                TenTextBox.Focus();
                return;
            }

            Result<CongViecNoiBoDto> result;

            if (Model.Id == Guid.Empty)
            {
                // CREATE
                result = await api.CreateAsync(Model);
            }

            else
            {
                // UPDATE
                result = await api.UpdateAsync(Model.Id, Model);
            }

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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                return;
            }

            if (e.Key == Key.Enter)
            {
                SaveButton_Click(null!, null!);
            }
        }

        // giữ để tránh warning XAML
        private void NhomSanPhamListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}