using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TraSuaApp.WpfClient.Controls
{
    public class NumericTextBox : TextBox
    {
        private static readonly Regex _nonDigitRegex = new(@"[^0-9]", RegexOptions.Compiled);
        private bool _isFormatting = false;

        // Lưu giá trị cũ để kiểm tra thay đổi
        private decimal _oldValue = 0m;

        public NumericTextBox()
        {
            PreviewTextInput += OnPreviewTextInput;
            TextChanged += OnTextChanged;
            GotFocus += OnGotFocus;
            LostFocus += OnLostFocus;
            DataObject.AddPastingHandler(this, OnPaste);
        }

        // Định nghĩa event ValueChanged
        public event RoutedPropertyChangedEventHandler<decimal>? ValueChanged;

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

            string raw = new string(Text.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(raw))
            {
                _isFormatting = false;
                SetValueAndRaiseEvent(0m);
                return;
            }

            if (long.TryParse(raw, out var number))
            {
                var newValue = (decimal)number;
                var formatted = number.ToString("N0", CultureInfo.CurrentCulture);
                int oldCaret = SelectionStart;
                Text = formatted;
                SelectionStart = Math.Min(Text.Length, oldCaret + (Text.Length - raw.Length));

                SetValueAndRaiseEvent(newValue);
            }

            _isFormatting = false;
        }

        private void SetValueAndRaiseEvent(decimal newValue)
        {
            if (_oldValue != newValue)
            {
                var oldVal = _oldValue;
                _oldValue = newValue;
                ValueChanged?.Invoke(this, new RoutedPropertyChangedEventArgs<decimal>(oldVal, newValue));
            }
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            string raw = new string(Text.Where(char.IsDigit).ToArray());
            Text = raw;
            SelectionStart = Text.Length;
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (long.TryParse(Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var v))
            {
                Text = v.ToString("N0", CultureInfo.CurrentCulture);
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
                if (_oldValue != value)
                {
                    Text = value == 0 ? "" : value.ToString("N0", CultureInfo.CurrentCulture);
                    SetValueAndRaiseEvent(value);
                }
            }
        }
    }
}