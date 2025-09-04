using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class NhomSanPhamEdit : Window
    {
        public NhomSanPhamDto Model { get; set; } = new();
        private readonly INhomSanPhamApi _api;
        string _friendlyName = TuDien._tableFriendlyNames["NhomSanPham"];

        public NhomSanPhamEdit(NhomSanPhamDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;


            _api = new NhomSanPhamApi();

            if (dto != null)
            {
                Model = dto;
                TenTextBox.Text = dto.Ten;
            }
            else
            {
                TenTextBox.Focus();
            }

            if (Model.IsDeleted)
            {
                TenTextBox.IsEnabled = false;
                SaveButton.Content = "Khôi phục";
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            Model.Ten = TenTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(Model.Ten))
            {
                ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                TenTextBox.Focus();
                return;
            }

            Result<NhomSanPhamDto> result;
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
                SaveButton_Click(null, null);

            }
        }
    }
}
