using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TraSuaApp.WpfClient.Helpers
{
    public class KindToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            string k = (value?.ToString() ?? "info").ToLowerInvariant();

            Brush F(string key, string fallbackHex)
            {
                var b = System.Windows.Application.Current.TryFindResource(key) as Brush;
                return b ?? (SolidColorBrush)new BrushConverter().ConvertFromString(fallbackHex);
            }

            return k switch
            {
                "success" => F("SuccessBrush", "#FF4CAF50"),
                "warn" => F("WarningBrush", "#FFFFC107"),
                "error" => F("DangerBrush", "#FFF44336"),
                _ => F("InfoBrush", "#FF2196F3"),
            };
        }

        public object ConvertBack(object v, Type t, object p, CultureInfo c) => Binding.DoNothing;
    }
}