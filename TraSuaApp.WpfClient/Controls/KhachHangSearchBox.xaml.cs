using System.Collections.ObjectModel;
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
        public ObservableCollection<KhachHangDto> KhachHangList = new();
        public KhachHangDto? SelectedKhachHang { get; private set; }

        // 🟟 Sự kiện mở rộng
        public event Action<KhachHangDto>? KhachHangSelected;
        public event Action<KhachHangDto>? KhachHangConfirmed;  // Double-click xác nhận chọn khách
        public event Action? KhachHangCleared;
        public event Action? KhachMoiSelected;                   // Chọn “Khách mới”

        // 🟟 Tuỳ chọn hành vi
        public bool ShowAllWhenEmpty { get; set; } = false;
        public bool SuppressPopup { get; set; } = false;
        public double? FixedPopupHeight
        {
            get => (double?)GetValue(FixedPopupHeightProperty);
            set => SetValue(FixedPopupHeightProperty, value);
        }

        public static readonly DependencyProperty FixedPopupHeightProperty =
            DependencyProperty.Register(nameof(FixedPopupHeight), typeof(double?), typeof(KhachHangSearchBox),
                new PropertyMetadata(null));
        public KhachHangSearchBox()
        {
            InitializeComponent();
        }

        public bool IsPopupOpen
        {
            get => Popup.IsOpen;
            set => Popup.IsOpen = value;
        }

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

        // ==========================
        // 🟟 Xử lý tìm kiếm / hiển thị popup
        // ==========================
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearButton.Visibility = string.IsNullOrWhiteSpace(SearchTextBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;

            string raw = SearchTextBox.Text?.Trim() ?? "";
            string keyword = StringHelper.MyNormalizeText(raw);

            List<KhachHangDto> results;

            if (string.IsNullOrEmpty(keyword))
            {
                if (ShowAllWhenEmpty)
                {
                    results = KhachHangList
                        .OrderByDescending(x => x.ThuTu)
                        .Take(30)
                        .ToList();
                }
                else
                {
                    ListBoxResults.ItemsSource = null;
                    Popup.IsOpen = false;
                    return;
                }
            }
            else
            {
                results = KhachHangList
                    .Where(x => x.TimKiem.Contains(keyword))
                    .OrderByDescending(x => x.ThuTu)
                    .Take(30)
                    .ToList();
            }



            ListBoxResults.ItemsSource = results;
            Popup.IsOpen = !SuppressPopup && results.Any();
        }
        public void TriggerSelectedEvent(KhachHangDto? kh)
        {
            if (kh != null && KhachHangSelected != null)
                KhachHangSelected.Invoke(kh);
        }
        // ==========================
        // 🟟 Chọn khách hàng
        // ==========================
        private void ListBoxResults_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ListBoxResults.SelectedItem is KhachHangDto kh)
            {
                Select(kh);

                // 🟟 Khi click chọn khách xong thì coi như xác nhận luôn
                KhachHangConfirmed?.Invoke(kh);
            }

            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }

        private void Select(KhachHangDto kh)
        {
            if (kh.Id == Guid.Empty)
            {
                // 🟟 Khách mới
                SelectedKhachHang = null;
                SearchTextBox.Text = "!!! Nếu là KHÁCH MỚI nhấn vào đây, khách cũ nhập để tìm !!!";
                Popup.IsOpen = false;
                KhachMoiSelected?.Invoke();
                return;
            }

            SelectedKhachHang = kh;
            SearchTextBox.Text = kh.Ten;
            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            if (!ShowAllWhenEmpty)
                Popup.IsOpen = false;
            KhachHangSelected?.Invoke(kh);
        }

        // ==========================
        // 🟟 Điều hướng bàn phím
        // ==========================
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

        // ==========================
        // 🟟 Nút ▲ ▼ (đổi thứ tự)
        // ==========================
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
                    // 🟟 Hoán đổi thứ tự
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