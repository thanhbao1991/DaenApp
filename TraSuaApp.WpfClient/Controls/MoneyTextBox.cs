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

        public MoneyTextBox()
        {
            PreviewTextInput += OnPreviewTextInput;
            TextChanged += OnTextChanged;
            DataObject.AddPastingHandler(this, OnPaste);
            LostFocus += OnLostFocus;
            GotFocus += OnGotFocus;

            // KHÔNG gán mặc định Text = "0"
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
            _isFormatting = true;

            string raw = _nonDigitRegex.Replace(Text, "");

            if (string.IsNullOrEmpty(raw))
            {
                _isFormatting = false;
                return;
            }

            int oldCaret = SelectionStart;

            if (long.TryParse(raw, out long number))
            {
                string formatted = number.ToString("N0", CultureInfo.CurrentCulture);
                Text = formatted;
                SelectionStart = CalculateCaretIndex(oldCaret, raw, formatted);
            }

            _isFormatting = false;
        }

        private int CalculateCaretIndex(int oldCaret, string raw, string formatted)
        {
            int rawLength = raw.Length;
            int formattedLength = formatted.Length;
            int offset = formattedLength - rawLength;
            return Math.Max(0, Math.Min(formattedLength, oldCaret + offset));
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            string raw = _nonDigitRegex.Replace(Text, "");
            Text = raw;
            SelectionStart = Text.Length;
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            string raw = _nonDigitRegex.Replace(Text, "");

            if (string.IsNullOrEmpty(raw)) return;

            if (long.TryParse(raw, out long number))
            {
                Text = number.ToString("N0", CultureInfo.CurrentCulture);
            }
        }

        public decimal Value
        {
            get
            {
                string raw = _nonDigitRegex.Replace(Text, "");
                return decimal.TryParse(raw, out decimal v) ? v : 0m;
            }
            set
            {
                Text = value == 0 ? "" : value.ToString("N0", CultureInfo.CurrentCulture);
            }
        }
    }
}
