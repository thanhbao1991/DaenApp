using System.Globalization;
using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.SettingsViews
{
    public partial class KhachHangGiaBanEdit : Window
    {
        public KhachHangGiaBanDto Model { get; set; } = new();
        private readonly IKhachHangGiaBanApi _api;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["KhachHangGiaBan"];

        public KhachHangGiaBanEdit(KhachHangGiaBanDto? dto = null)
        {
            InitializeComponent();
            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            KeyDown += Window_KeyDown;

            _api = new KhachHangGiaBanApi();
            if (dto != null) Model = dto;

            Loaded += (_, __) =>
            {

                if (Model.IsDeleted)
                {
                    GiaBanTextBox.IsEnabled = false;
                    SaveButton.Content = "Khôi phục";
                }

                // ❗Block create
                if (Model.Id == Guid.Empty)
                {
                    SaveButton.IsEnabled = false;
                    CreateWarningText.Visibility = Visibility.Visible;
                    GiaBanTextBox.IsEnabled = false;
                }
                else
                {
                    GiaBanTextBox.Focus();
                    GiaBanTextBox.SelectAll();
                }
            };
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            if (Model.IsDeleted)
            {
                var restored = await _api.RestoreAsync(Model.Id);
                if (!restored.IsSuccess) { ErrorTextBlock.Text = restored.Message; return; }
                DialogResult = true; Close(); return;
            }

            Model.GiaBan = GiaBanTextBox.Value;

            // chỉ đổi giá
            var payload = new KhachHangGiaBanDto
            {
                Id = Model.Id,
                GiaBan = Model.GiaBan,
                SanPhamBienTheId = Model.SanPhamBienTheId,
                KhachHangId = Model.KhachHangId,
                LastModified = Model.LastModified
            };

            var result = await _api.UpdateAsync(Model.Id, payload);
            if (!result.IsSuccess) { ErrorTextBlock.Text = result.Message; return; }

            DialogResult = true;
            Close();
        }

        private static bool TryParseDecimal(string? input, out decimal value)
        {
            input = (input ?? "").Trim().Replace(",", ".");
            return decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            else if (e.Key == Key.Enter && SaveButton.IsEnabled) SaveButton_Click(null!, null!);
        }

        private void GiaBanTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var ch = e.Text;
            e.Handled = !(char.IsDigit(ch, 0) || ch == "," || ch == ".");
        }
        private void GiaBanTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
            => ErrorTextBlock.Text = "";
    }
}