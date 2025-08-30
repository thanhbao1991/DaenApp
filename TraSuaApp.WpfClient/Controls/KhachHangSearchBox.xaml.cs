using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
namespace TraSuaApp.WpfClient.Controls
{
    public partial class KhachHangSearchBox : UserControl
    {
        public List<KhachHangDto> KhachHangList { get; set; } = new();
        public KhachHangDto? SelectedKhachHang { get; private set; }
        public event Action<KhachHangDto>? KhachHangSelected;
        public event Action? KhachHangCleared;

        public KhachHangSearchBox()
        {
            InitializeComponent();
        }

        public bool IsPopupOpen => ListBoxResults.Items.Count > 0;
        public bool SuppressPopup { get; set; } = false;

        public void SetSelectedKhachHang(KhachHangDto kh)
        {
            SelectedKhachHang = kh;
            SearchTextBox.Text = kh.Ten;
        }

        public void SetTextWithoutPopup(string text)
        {
            SuppressPopup = true;
            SearchTextBox.Text = text;
            SuppressPopup = false;
        }

        public void SetSelectedKhachHangByIdWithoutPopup(Guid id)
        {
            SuppressPopup = true;
            SetSelectedKhachHangById(id);
            SuppressPopup = false;
        }

        public void SetSelectedKhachHangById(Guid id)
        {
            var kh = KhachHangList.FirstOrDefault(x => x.Id == id);
            if (kh != null)
                SetSelectedKhachHang(kh);
            else
            {
                SelectedKhachHang = null;
                SearchTextBox.Text = "";
                Popup.IsOpen = false;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            SelectedKhachHang = null;
            ClearButton.Visibility = Visibility.Collapsed;
            KhachHangCleared?.Invoke();
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

            // Hàm tạo chữ cái viết tắt (acronym) từ tên
            string GetInitials(string name)
            {
                if (string.IsNullOrWhiteSpace(name)) return "";
                var words = TextSearchHelper.NormalizeText(name)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return string.Concat(words.Select(w => w[0])); // ví dụ "Xuân Hải" -> "xh"
            }

            var results = KhachHangList
                .Select(kh =>
                {
                    int score = 0;
                    var ten = TextSearchHelper.NormalizeText(kh.Ten ?? "");
                    var initials = GetInitials(kh.Ten ?? "");
                    var sdt = TextSearchHelper.NormalizeText(kh.DienThoai ?? "");
                    var diaChi = TextSearchHelper.NormalizeText(kh.DiaChi ?? "");
                    var timKiem = TextSearchHelper.NormalizeText(kh.TimKiem ?? "");

                    // Ưu tiên viết tắt
                    if (initials == keyword) score += 500;
                    else if (initials.StartsWith(keyword)) score += 400;

                    // Tên
                    if (ten.StartsWith(keyword)) score += 300;
                    else if (ten.Contains(keyword)) score += 200;

                    // SĐT
                    if (!string.IsNullOrEmpty(sdt))
                    {
                        if (sdt.StartsWith(keyword)) score += 350;
                        else if (sdt.Contains(keyword)) score += 150;
                    }

                    // Địa chỉ
                    if (!string.IsNullOrEmpty(diaChi) && diaChi.Contains(keyword))
                        score += 100;

                    // Fallback TimKiem
                    if (!string.IsNullOrEmpty(timKiem) && timKiem.Contains(keyword))
                        score += 50;

                    return new { KhachHang = kh, Score = score };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.KhachHang.Ten)
                .Take(50)
                .Select(x => x.KhachHang)
                .ToList();

            ListBoxResults.ItemsSource = results;
            Popup.IsOpen = !SuppressPopup && results.Any();
        }

        private void ListBoxResults_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ListBoxResults.SelectedItem is KhachHangDto kh)
                Select(kh);
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }

        private void Select(KhachHangDto kh)
        {
            SelectedKhachHang = kh;
            SearchTextBox.Text = kh.Ten;
            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            Popup.IsOpen = false;
            KhachHangSelected?.Invoke(kh);
        }

        private void SearchBox_And_ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender == SearchTextBox && e.Key == Key.Down && ListBoxResults.Items.Count > 0)
            {
                ListBoxResults.Focus();
                ListBoxResults.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                KhachHangDto? kh = null;
                if (sender == SearchTextBox && ListBoxResults.Items.Count > 0)
                    kh = ListBoxResults.Items[0] as KhachHangDto;
                else if (sender == ListBoxResults && ListBoxResults.SelectedItem is KhachHangDto selected)
                    kh = selected;

                if (kh != null)
                {
                    Select(kh);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                Popup.IsOpen = false;
                SearchTextBox.Focus();
                KhachHangCleared?.Invoke();
                e.Handled = true;
            }
        }
    }
}