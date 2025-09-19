

using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TraSuaApp.WpfClient.Controls
{
    public class MoneyTextBox : TextBox
    {
        private static readonly Regex _nonDigitRegex = new(@"[^\d]", RegexOptions.Compiled);
        private bool _isFormatting = false;

        // 🟟 DependencyProperty để binding DonGia
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(decimal),
                typeof(MoneyTextBox),
                new FrameworkPropertyMetadata(
                    default(decimal),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnValueChanged
                )
            );

        public decimal Value
        {
            get => (decimal)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MoneyTextBox mtb)
            {
                if (mtb._isFormatting) return;

                mtb._isFormatting = true;
                var number = (decimal)e.NewValue;
                mtb.Text = number == 0 ? "" : number.ToString("N0", CultureInfo.CurrentCulture);
                mtb.SelectionStart = mtb.Text.Length;
                mtb._isFormatting = false;
            }
        }

        public MoneyTextBox()
        {
            FontWeight = FontWeights.Medium;

            PreviewTextInput += OnPreviewTextInput;
            TextChanged += OnTextChanged;
            DataObject.AddPastingHandler(this, OnPaste);
        }

        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _nonDigitRegex.IsMatch(e.Text);
        }

        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var pasted = (string)e.DataObject.GetData(typeof(string))!;
                if (_nonDigitRegex.IsMatch(pasted))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;

            string raw = _nonDigitRegex.Replace(Text, "");
            if (string.IsNullOrEmpty(raw))
            {
                Value = 0;
                return;
            }

            if (decimal.TryParse(raw, out decimal number))
            {
                _isFormatting = true;

                Value = number;

                string formatted = number.ToString("N0", CultureInfo.CurrentCulture);

                // Giữ caret không bị nhảy về cuối
                int oldCaret = SelectionStart;
                int offset = formatted.Length - Text.Length;

                Text = formatted;
                SelectionStart = Math.Max(0, Math.Min(formatted.Length, oldCaret + offset));

                _isFormatting = false;
            }
        }
    }
}