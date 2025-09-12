using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Services;
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

        public bool IsPopupOpen
        {
            get => Popup.IsOpen;
            set => Popup.IsOpen = value;
        }

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

        //        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        //        {
        //            ClearButton.Visibility = string.IsNullOrWhiteSpace(SearchTextBox.Text)
        //                ? Visibility.Collapsed
        //                : Visibility.Visible;

        //            string keyword = TextSearchHelper.NormalizeText(SearchTextBox.Text.Trim());
        //            if (string.IsNullOrEmpty(keyword))
        //            {
        //                ListBoxResults.ItemsSource = null;
        //                Popup.IsOpen = false;
        //                return;
        //            }

        //            // Hàm tạo chữ cái viết tắt (acronym) từ tên
        //            string GetInitials(string name)
        //            {
        //                if (string.IsNullOrWhiteSpace(name)) return "";
        //                var words = TextSearchHelper.NormalizeText(name)
        //                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //                return string.Concat(words.Select(w => w[0])); // ví dụ "Xuân Hải" -> "xh"
        //            }

        //            var results = KhachHangList
        //                .Select(kh =>
        //                {
        //                    int score = 0;
        //                    var ten = TextSearchHelper.NormalizeText(kh.Ten ?? "");
        //                    var initials = GetInitials(kh.Ten ?? "");
        //                    var sdt = TextSearchHelper.NormalizeText(kh.DienThoai ?? "");
        //                    var diaChi = TextSearchHelper.NormalizeText(kh.DiaChi ?? "");
        //                    var timKiem = TextSearchHelper.NormalizeText(kh.TimKiem ?? "");

        //                    // Ưu tiên viết tắt
        //                    if (initials == keyword) score += 500;
        //                    else if (initials.StartsWith(keyword)) score += 400;

        //                    // Tên
        //                    if (ten.StartsWith(keyword)) score += 300;
        //                    else if (ten.Contains(keyword)) score += 200;

        //                    // SĐT
        //                    if (!string.IsNullOrEmpty(sdt))
        //                    {
        //                        if (sdt.StartsWith(keyword)) score += 350;
        //                        else if (sdt.Contains(keyword)) score += 150;
        //                    }

        //                    // Địa chỉ
        //                    if (!string.IsNullOrEmpty(diaChi) && diaChi.Contains(keyword))
        //                        score += 100;

        //                    // Fallback TimKiem
        //                    if (!string.IsNullOrEmpty(timKiem) && timKiem.Contains(keyword))
        //                        score += 50;

        //                    return new { KhachHang = kh, Score = score };
        //                })
        //                .Where(x => x.Score > 0)
        //          .OrderByDescending(x => x.Score)
        //.ThenByDescending(x => x.KhachHang.ThuTu)

        //                .Take(20)
        //                .Select(x => x.KhachHang)
        //                .ToList();

        //            ListBoxResults.ItemsSource = results;
        //            Popup.IsOpen = !SuppressPopup && results.Any();
        //        }

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

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearButton.Visibility = string.IsNullOrWhiteSpace(SearchTextBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;

            string raw = SearchTextBox.Text?.Trim() ?? "";
            string keyword = TextSearchHelper.NormalizeText(raw);

            if (string.IsNullOrEmpty(keyword))
            {
                ListBoxResults.ItemsSource = null;
                Popup.IsOpen = false;
                return;
            }

            var results = KhachHangList
                .Select(kh =>
                {
                    int score = 0;

                    foreach (var token in kh.TimKiemTokens)
                    {
                        if (token == keyword) score += 500;
                        else if (token.StartsWith(keyword)) score += 300;
                        else if (token.Contains(keyword)) score += 100;
                    }

                    return new { kh, score };
                })

                   .Where(x => x.score > 0)
                .OrderByDescending(x => x.kh.ThuTu)
                .Select(x => x.kh)
                .ToList();

            ListBoxResults.ItemsSource = results;
            Popup.IsOpen = !SuppressPopup && results.Any();
        }

        private void SearchBox_And_ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (sender == SearchTextBox && e.Key == Key.Down && ListBoxResults.Items.Count > 0)
            //{
            //    ListBoxResults.Focus();
            //    ListBoxResults.SelectedIndex = 0;
            //    e.Handled = true;
            //}
            //else if (e.Key == Key.Enter)
            //{
            //    KhachHangDto? kh = null;

            //    if (sender == SearchTextBox && ListBoxResults.Items.Count > 0)
            //        kh = ListBoxResults.Items[0] as KhachHangDto;
            //    else if (sender == ListBoxResults && ListBoxResults.SelectedItem is KhachHangDto selected)
            //        kh = selected;

            //    if (kh != null)
            //    {
            //        Select(kh);            // chọn khách hàng
            //        Popup.IsOpen = false;  // đóng popup
            //        e.Handled = true;      // chặn không cho Enter chạy lên Window
            //    }
            //}
            //else if (e.Key == Key.Escape)
            //{
            //    Popup.IsOpen = false;
            //    SearchTextBox.Focus();
            //    KhachHangCleared?.Invoke();
            //    e.Handled = true;
            //}
        }
        private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && Popup.IsOpen && ListBoxResults.Items.Count > 0)
            {
                if (SearchTextBox.IsKeyboardFocusWithin)
                {
                    ListBoxResults.Focus();
                    ListBoxResults.SelectedIndex = 0;
                    e.Handled = true;
                }
                else if (ListBoxResults.IsKeyboardFocusWithin)
                {
                    int index = ListBoxResults.SelectedIndex;
                    if (index < ListBoxResults.Items.Count - 1)
                    {
                        ListBoxResults.SelectedIndex = index + 1;
                        ListBoxResults.ScrollIntoView(ListBoxResults.SelectedItem);
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == Key.Up && Popup.IsOpen && ListBoxResults.Items.Count > 0)
            {
                if (ListBoxResults.IsKeyboardFocusWithin)
                {
                    int index = ListBoxResults.SelectedIndex;
                    if (index > 0)
                    {
                        ListBoxResults.SelectedIndex = index - 1;
                        ListBoxResults.ScrollIntoView(ListBoxResults.SelectedItem);
                        e.Handled = true;
                    }
                    else
                    {
                        SearchTextBox.Focus();
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (Popup.IsOpen)
                {
                    KhachHangDto? item = null;

                    if (ListBoxResults.SelectedItem is KhachHangDto selected)
                        item = selected;
                    else if (ListBoxResults.Items.Count > 0)
                        item = ListBoxResults.Items[0] as KhachHangDto;

                    if (item != null)
                    {
                        Select(item);
                        Popup.IsOpen = false;
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == Key.Escape)
            {
                if (Popup.IsOpen)
                {
                    Popup.IsOpen = false;
                    SearchTextBox.Focus();
                    KhachHangCleared?.Invoke();
                    e.Handled = true;
                }
            }
        }

        private async void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid khId)
            {
                var results = ListBoxResults.ItemsSource as List<KhachHangDto>;
                if (results == null) return;

                var current = results.FirstOrDefault(r => r.Id == khId);
                if (current == null) return;

                int index = results.FindIndex(r => r.Id == khId);
                KhachHangDto? neighbor = null;

                if (btn.Name == "UpButton" && index > 0)
                {
                    neighbor = results[index - 1];
                }
                else if (btn.Name != "UpButton" && index < results.Count - 1)
                {
                    neighbor = results[index + 1];
                }

                if (neighbor != null)
                {
                    // 🟟 Hoán đổi ThuTu
                    int temp = current.ThuTu;
                    current.ThuTu = neighbor.ThuTu;
                    neighbor.ThuTu = temp;

                    current.LastModified = DateTime.Now;
                    neighbor.LastModified = DateTime.Now;

                    var api = new KhachHangApi();
                    var result1 = await api.UpdateSingleAsync(current.Id, current);
                    var result2 = await api.UpdateSingleAsync(neighbor.Id, neighbor);

                    if (result1.IsSuccess && result2.IsSuccess)
                    {
                        SearchTextBox_TextChanged(null, null);
                    }
                    else
                    {
                        MessageBox.Show("Có lỗi khi cập nhật", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}