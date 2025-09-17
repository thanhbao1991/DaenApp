
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TraSuaApp.WpfClient.Converters
{
    public class RowStyleMultiConverter : IMultiValueConverter
    {
        private SolidColorBrush GetBrush(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as SolidColorBrush ?? Brushes.Transparent;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var ngayShip = values[0];
            var phanLoai = values[1]?.ToString();
            var trangThai = values[2]?.ToString();
            bool DaThuHoacGhiNo = false;
            bool.TryParse(values[3]?.ToString(), out DaThuHoacGhiNo);

            if (parameter?.ToString() == "Foreground")
            {
                if (phanLoai == "Ship")
                {
                    if (ngayShip == null)
                        return GetBrush("InfoBrush");        // LightSkyBlue → Info
                    else
                        return DaThuHoacGhiNo
                            ? Brushes.Transparent
                            : GetBrush("PrimaryBrush");     // LightBlue → Primary
                }
                if (phanLoai == "App" && !DaThuHoacGhiNo)
                    return GetBrush("DangerBrush");          // LightPink → Danger
                if (phanLoai == "Tại Chỗ" && !DaThuHoacGhiNo)
                    return GetBrush("SuccessBrush");         // LightGreen → Success
                if (phanLoai == "Mv" && !DaThuHoacGhiNo)
                    return GetBrush("WarningBrush");         // LightYellow → Warning

                return Brushes.Transparent;
            }

            if (parameter?.ToString() == "FontWeight")
            {
                if (phanLoai == "Ship")
                {
                    if (ngayShip == null)
                        return FontWeights.Medium;
                    else
                        return DaThuHoacGhiNo ? FontWeights.Normal : FontWeights.Medium;
                }
                if ((phanLoai == "App" || phanLoai == "Tại Chỗ" || phanLoai == "Mv") && !DaThuHoacGhiNo)
                    return FontWeights.Medium;

                return FontWeights.Normal;
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}