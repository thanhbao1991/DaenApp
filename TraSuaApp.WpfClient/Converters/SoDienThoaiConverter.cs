using System.Globalization;
using System.Windows.Data;

namespace TraSuaApp.WpfClient.Converters
{
    public class SoDienThoaiConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var so = (value as string)?.Replace(" ", "") ?? "";

            if (so.Length == 10 || so.Length == 11)
            {
                if (so.Length >= 4 + 3 + 3)
                    return $"{so[..4]} {so.Substring(4, 3)} {so.Substring(7)}";
            }

            return so;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString()?.Replace(" ", "") ?? "";
        }
    }
}