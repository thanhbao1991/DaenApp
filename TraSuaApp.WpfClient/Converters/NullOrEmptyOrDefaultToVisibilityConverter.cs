using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TraSuaApp.WpfClient.Converters
{
    public class NullOrEmptyOrDefaultToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return Visibility.Collapsed;

            if (value is string str)
            {
                return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
            }

            if (value is DateTime dt)
            {
                return dt == default ? Visibility.Collapsed : Visibility.Visible;
            }

            return Visibility.Visible; // Các kiểu khác mặc định hiển thị
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
