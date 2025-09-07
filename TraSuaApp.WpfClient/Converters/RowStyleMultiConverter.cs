using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace TraSuaApp.WpfClient.Converters
{
    public class BoolToRowDetailsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCard = (bool)(value ?? false);
            return isCard
                ? DataGridRowDetailsVisibilityMode.Visible
                : DataGridRowDetailsVisibilityMode.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverterWithInvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isCard = (bool)(value ?? false);
            bool invert = parameter?.ToString() == "Invert";

            if (invert) isCard = !isCard;

            return isCard ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }





    public class RowStyleMultiConverter : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var ngayShip = values[0];
            var phanLoai = values[1]?.ToString();
            var trangThai = values[2]?.ToString();
            var DaThuHoacGhiNo = bool.Parse(values[3]?.ToString());

            if (parameter?.ToString() == "Foreground")
            {
                if (phanLoai == "Ship")
                {
                    if (ngayShip == null)
                        return Brushes.LightSkyBlue;
                    else
                    {
                        if (DaThuHoacGhiNo)
                            return Brushes.Transparent;
                        else
                            return Brushes.LightBlue;
                    }
                }
                if (phanLoai == "App" && !DaThuHoacGhiNo)
                    return Brushes.LightPink;
                if (phanLoai == "Tại Chỗ" && !DaThuHoacGhiNo)
                    return Brushes.LightGreen;
                if (phanLoai == "MV" && !DaThuHoacGhiNo)
                    return Brushes.LightYellow;
                return Brushes.Transparent;
            }

            // điều kiện font weight
            if (parameter?.ToString() == "FontWeight")
            {
                if (phanLoai == "Ship")
                {
                    if (ngayShip == null)
                        return FontWeights.Medium;
                    else
                    {
                        if (DaThuHoacGhiNo)
                            return FontWeights.Normal;
                        else
                            return FontWeights.Medium;
                    }
                }
                if (phanLoai == "App" && !DaThuHoacGhiNo)
                    return FontWeights.Medium;
                if (phanLoai == "Tại Chỗ" && !DaThuHoacGhiNo)
                    return FontWeights.Medium;
                if (phanLoai == "MV" && !DaThuHoacGhiNo)
                    return FontWeights.Medium;
                return FontWeights.Normal;
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}