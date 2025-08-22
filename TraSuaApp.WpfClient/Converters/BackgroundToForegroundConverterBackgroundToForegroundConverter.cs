using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace TraSuaApp.WpfClient.Converters
{
    public class BackgroundToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                var color = brush.Color;
                double brightness = (299 * color.R + 587 * color.G + 114 * color.B) / 1000.0;
                return brightness > 128 ? Brushes.Black : Brushes.White;
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}