using System.Globalization;
using System.Windows.Data;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.WpfClient.Converters
{
    public class SkipFirstConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<SanPhamBienTheDto> list)
                return list.Skip(1).ToList();
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}