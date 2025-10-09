using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class TuDienTraCuuEdit : Window
    {
        public TuDienTraCuuDto Model { get; set; } = new();
        private readonly ITuDienTraCuuApi _api;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["TuDienTraCuu"];

        public TuDienTraCuuEdit(TuDienTraCuuDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _api = new TuDienTraCuuApi();

            // Load danh sách nhóm SP

            if (dto != null)
            {
                Model = dto;
                TenTextBox.Text = dto.Ten;
                TenPhienDichTextBox.Text = dto.TenPhienDich;
                DangSuDungCheckBox.IsChecked = dto.DangSuDung;


                // Kiểm tra Deleted
                if (Model.IsDeleted)
                {
                    TenTextBox.IsEnabled = false;
                    SaveButton.Content = "Khôi phục";
                }
            }
            else
            {
                // Giá trị mặc định
                DangSuDungCheckBox.IsChecked = true;
                TenTextBox.Focus();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            // Bắt lỗi trống tên
            if (string.IsNullOrWhiteSpace(TenTextBox.Text))
            {
                ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                TenTextBox.Focus();
                return;
            }

            Model.Ten = (TenTextBox.Text).Trim();
            Model.TenPhienDich = (TenPhienDichTextBox.Text).Trim();
            Model.DangSuDung = DangSuDungCheckBox.IsChecked == true;

            Result<TuDienTraCuuDto> result;
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
            if (e.Key == Key.Enter



                )
            {
                SaveButton_Click(null, null);

            }
        }
    }

}
