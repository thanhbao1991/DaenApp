using System.Globalization;
using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class LocationEdit : Window
    {
        public LocationDto Model { get; set; } = new();
        private readonly ILocationApi _api;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["Location"];

        public LocationEdit(LocationDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _api = new LocationApi();

            if (dto != null)
            {
                Model = dto;
                StartAddressTextBox.Text = Model.StartAddress;
                StartLatTextBox.Text = Model.StartLat?.ToString(CultureInfo.InvariantCulture) ?? "";
                StartLongTextBox.Text = Model.StartLong?.ToString(CultureInfo.InvariantCulture) ?? "";
                DistanceKmTextBox.Text = Model.DistanceKm?.ToString(CultureInfo.InvariantCulture) ?? "";
                MoneyDistanceTextBox.Text = Model.MoneyDistance?.ToString(CultureInfo.InvariantCulture) ?? "";
                MatrixTextBox.Text = Model.Matrix ?? "";
            }
            else
            {
                StartAddressTextBox.Focus();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            if (string.IsNullOrWhiteSpace(StartAddressTextBox.Text))
            {
                ErrorTextBlock.Text = $"Địa chỉ {_friendlyName.ToLower()} không được để trống.";
                StartAddressTextBox.Focus();
                return;
            }

            // parse số
            double? lat = TryParseDouble(StartLatTextBox.Text);
            double? lng = TryParseDouble(StartLongTextBox.Text);
            double? distKm = TryParseDouble(DistanceKmTextBox.Text);
            decimal? moneyDist = TryParseDecimal(MoneyDistanceTextBox.Text);

            Model.StartAddress = StartAddressTextBox.Text.Trim();
            Model.StartLat = lat;
            Model.StartLong = lng;
            Model.DistanceKm = distKm;
            Model.MoneyDistance = moneyDist;
            Model.Matrix = string.IsNullOrWhiteSpace(MatrixTextBox.Text) ? null : MatrixTextBox.Text.Trim();

            Result<LocationDto> result;
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

        private static double? TryParseDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
            if (double.TryParse(s, NumberStyles.Float, CultureInfo.CurrentCulture, out v)) return v;
            return null;
        }

        private static decimal? TryParseDecimal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var v)) return v;
            if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.CurrentCulture, out v)) return v;
            return null;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { CloseButton_Click(null!, null!); return; }
            if (e.Key == Key.Enter) { SaveButton_Click(null, null); }
        }
    }
}
