using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace TraSuaApp.WpfClient.Controls
{
    public class PhoneNumberTextBox : TextBox
    {
        private static readonly Regex _digitRegex = new(@"[^\d]", RegexOptions.Compiled);
        private bool _isFormatting;

        public PhoneNumberTextBox()
        {
            this.PreviewTextInput += OnPreviewTextInput;
            this.TextChanged += OnTextChanged;
            this.LostFocus += OnLostFocus;
            this.PreviewKeyDown += OnPreviewKeyDown;
            DataObject.AddPastingHandler(this, OnPaste);

            this.Text = "";
        }

        // Ngăn nhập ký tự không phải số
        private void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = _digitRegex.IsMatch(e.Text);
        }

        // Ngăn dán nếu có ký tự không phải số
        private void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string))!;
                if (_digitRegex.IsMatch(text))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        // Ngăn nhấn phím cách
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        // Format lại khi mất focus
        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            string digits = GetDigitsOnly(this.Text);
            this.Text = FormatPhoneNumber(digits);
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isFormatting) return;

            _isFormatting = true;

            string rawText = this.Text ?? "";

            // Tính vị trí chữ số tính đến vị trí con trỏ
            int digitCaret = GetDigitsOnly(rawText[..this.CaretIndex]).Length;

            // Bỏ các ký tự không phải số
            string digits = GetDigitsOnly(rawText);

            // Format lại
            string formatted = FormatPhoneNumber(digits);
            this.Text = formatted;

            // Gán lại vị trí con trỏ dựa theo digitCaret
            this.CaretIndex = GetCaretFromDigitIndex(formatted, digitCaret);

            _isFormatting = false;
        }

        // Lấy lại chỉ các chữ số
        private string GetDigitsOnly(string input)
        {
            return _digitRegex.Replace(input ?? "", "");
        }

        // Format thành dạng 4-3-3 (ví dụ 0889 664 007)
        private string FormatPhoneNumber(string digits)
        {
            if (string.IsNullOrEmpty(digits))
                return "";

            if (digits.Length <= 4)
                return digits;
            else if (digits.Length <= 7)
                return $"{digits[..4]} {digits[4..]}";
            else if (digits.Length <= 10)
                return $"{digits[..4]} {digits[4..7]} {digits[7..]}";
            else
                return $"{digits[..4]} {digits[4..7]} {digits[7..10]}"; // max 10 số
        }

        // Tính caret từ vị trí chữ số
        private int GetCaretFromDigitIndex(string formatted, int digitIndex)
        {
            int count = 0;
            for (int i = 0; i < formatted.Length; i++)
            {
                if (char.IsDigit(formatted[i]))
                    count++;

                if (count == digitIndex)
                    return i + 1;
            }

            return formatted.Length;
        }

        // Lấy giá trị số điện thoại dạng thô (chỉ số)
        public string RawPhoneNumber => GetDigitsOnly(this.Text);
    }
}
