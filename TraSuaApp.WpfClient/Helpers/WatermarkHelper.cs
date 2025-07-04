// WatermarkHelper.cs
using System.Windows;

namespace TraSuaApp.WpfClient.Helpers
{
    public class WatermarkHelper
    {
        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.RegisterAttached(
                "Placeholder",
                typeof(string),
                typeof(WatermarkHelper),
                new FrameworkPropertyMetadata(string.Empty));

        public static void SetPlaceholder(UIElement element, string value)
            => element.SetValue(PlaceholderProperty, value);

        public static string GetPlaceholder(UIElement element)
            => (string)element.GetValue(PlaceholderProperty);
    }
}