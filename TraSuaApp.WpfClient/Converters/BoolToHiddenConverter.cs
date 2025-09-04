using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TraSuaApp.WpfClient.Converters
{
    public class BoolToHiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b)
                return Visibility.Hidden;   // giữ chỗ, không hiển thị
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is Visibility v && v == Visibility.Hidden);
        }
    }
}