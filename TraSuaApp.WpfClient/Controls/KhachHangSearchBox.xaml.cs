using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class KhachHangSearchBox : UserControl
    {
        public ObservableCollection<KhachHangDto> KhachHangList = new();
        public KhachHangDto? SelectedKhachHang { get; private set; }

        public event Action<KhachHangDto>? KhachHangSelected;
        public event Action<KhachHangDto>? KhachHangConfirmed;
        public event Action? KhachHangCleared;
        public event Action? KhachMoiSelected;

        public bool TrySelectUniqueMatchFromText(bool fireConfirmedEvent = true)
        {
            string raw = SearchTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(raw)) return false;

            string keyword = StringHelper.MyNormalizeText(raw);

            var results = KhachHangList
                .Where(x => x.TimKiem.Contains(keyword))
                .OrderByDescending(x => x.ThuTu)
                .Take(2)
                .ToList();

            if (results.Count == 1)
            {
                var kh = results[0];

                bool old = SuppressPopup;
                SuppressPopup = true;
                try
                {
                    Select(kh);
                    if (fireConfirmedEvent) KhachHangConfirmed?.Invoke(kh);
                }
                finally
                {
                    SuppressPopup = old;
                }
                return true;
            }

            return false;
        }

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
                    .Take(15)
                    .ToList();
            }

            ListBoxResults.ItemsSource = results;
            Popup.IsOpen = !SuppressPopup && results.Any();
        }

        public void TriggerSelectedEvent(KhachHangDto? kh)
        {
            if (kh != null)
                KhachHangSelected?.Invoke(kh);
        }

        private void ListBoxResults_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ListBoxResults.SelectedItem is KhachHangDto kh)
            {
                Select(kh);
                KhachHangConfirmed?.Invoke(kh);
            }

            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }

        private void Select(KhachHangDto kh)
        {
            if (kh.Id == Guid.Empty)
            {
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
                    KhachHangDto? item =
                        ListBoxResults.SelectedItem as KhachHangDto
                        ?? ListBoxResults.Items[0] as KhachHangDto;

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

        // ================= MOVE =================
        private async void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid khId)
            {
                var results = ListBoxResults.ItemsSource as List<KhachHangDto>;
                if (results == null) return;

                int index = results.FindIndex(r => r.Id == khId);
                if (index < 0) return;

                var current = results[index];
                KhachHangDto? neighbor = null;

                if (btn.Name == "UpButton" && index > 0)
                    neighbor = results[index - 1];
                else if (btn.Name != "UpButton" && index < results.Count - 1)
                    neighbor = results[index + 1];

                if (neighbor == null) return;

                (current.ThuTu, neighbor.ThuTu) = (neighbor.ThuTu, current.ThuTu);

                current.LastModified = DateTime.Now;
                neighbor.LastModified = DateTime.Now;

                var api = Apis.KhachHang;

                var result1 = await api.UpdateAsync(current.Id, current);
                var result2 = await api.UpdateAsync(neighbor.Id, neighbor);

                if (result1.IsSuccess && result2.IsSuccess)
                {
                    SearchTextBox_TextChanged(null!, null!);
                }
                else
                {
                    MessageBox.Show("Có lỗi khi cập nhật", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}