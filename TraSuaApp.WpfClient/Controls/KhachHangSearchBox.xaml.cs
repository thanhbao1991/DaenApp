using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class KhachHangSearchBox : UserControl
    {
        // Public API:
        public List<KhachHangDto> KhachHangList { get; set; } = new();
        public KhachHangDto? SelectedKhachHang { get; private set; }
        public event Action<KhachHangDto>? KhachHangSelected;
        public event Action? KhachHangCleared;

        public KhachHangSearchBox()
        {
            InitializeComponent();
        }
        private void SearchBox_And_ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Nếu đang ở TextBox và nhấn phím xuống
            if (sender == SearchTextBox && e.Key == Key.Down && ListBoxResults.Items.Count > 0)
            {
                ListBoxResults.Focus();
                ListBoxResults.SelectedIndex = 0;
                e.Handled = true;
            }
            // Nếu nhấn Enter
            else if (e.Key == Key.Enter)
            {
                KhachHangDto? kh = null;

                if (sender == SearchTextBox && ListBoxResults.Items.Count > 0)
                {
                    // Lấy kết quả đầu tiên
                    kh = ListBoxResults.Items[0] as KhachHangDto;
                }
                else if (sender == ListBoxResults && ListBoxResults.SelectedItem is KhachHangDto selected)
                {
                    // Lấy mục đang chọn
                    kh = selected;
                }

                if (kh != null)
                {
                    Select(kh);
                    e.Handled = true;
                }
            }
            // Nếu nhấn Escape
            else if (e.Key == Key.Escape)
            {
                Popup.IsOpen = false;
                SearchTextBox.Focus();
                KhachHangCleared?.Invoke();
                e.Handled = true;
            }
        }

        // Expose for HoaDonEdit to pre-select
        public void SetSelectedKhachHang(KhachHangDto kh)
        {
            SelectedKhachHang = kh;
            SearchTextBox.Text = kh.Ten;
        }

        // Internal state for Popup
        public bool IsPopupOpen
            => ListBoxResults.Items.Count > 0;

        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
        }

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && ListBoxResults.Items.Count > 0)
            {
                ListBoxResults.Focus();
                ListBoxResults.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Popup.IsOpen = false;
                KhachHangCleared?.Invoke();
            }
        }
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            ClearButton.Visibility = Visibility.Collapsed;
            // Bạn có thể raise event để thông báo đã huỷ chọn nếu cần
            KhachHangCleared?.Invoke();
            //
            SearchTextBox.Focus();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearButton.Visibility = string.IsNullOrWhiteSpace(SearchTextBox.Text)
        ? Visibility.Collapsed
        : Visibility.Visible;


            string keyword = TextSearchHelper.NormalizeText(SearchTextBox.Text.Trim());
            if (string.IsNullOrEmpty(keyword))
            {
                ListBoxResults.ItemsSource = null;
                Popup.IsOpen = false;
                return;
            }

            var parts = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var result = KhachHangList
                .Where(kh => kh.TimKiem.Contains(keyword)
                             || parts.All(p => kh.TimKiem.Contains(p)))
                .Take(50)
                .ToList();

            ListBoxResults.ItemsSource = result;
            Popup.IsOpen = result.Any();
        }

        private void ListBoxResults_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ListBoxResults.SelectedItem is KhachHangDto kh)
                Select(kh);
            // SearchTextBox.Focus();
            //SearchTextBox.SelectAll();
        }

        private void ListBoxResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // optional: if you want selection change by keyboard
        }

        private void Select(KhachHangDto kh)
        {
            SelectedKhachHang = kh;
            SearchTextBox.Text = kh.Ten;
            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            Popup.IsOpen = false;
            KhachHangSelected?.Invoke(kh);
        }
    }
}
