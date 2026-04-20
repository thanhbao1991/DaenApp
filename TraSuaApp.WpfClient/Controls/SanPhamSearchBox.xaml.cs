using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Controls
{
    public class SanPhamSearchResult
    {
        public Guid SanPhamId { get; set; }
        public string TenSanPham { get; set; } = "";
        public List<SanPhamBienTheDto> BienThes { get; set; } = new();
    }

    public partial class SanPhamSearchBox : UserControl
    {
        public List<SanPhamDto> SanPhamList { get; set; } = new();

        public SanPhamDto? SelectedSanPham { get; private set; }
        public SanPhamBienTheDto? SelectedBienThe { get; private set; }

        public event Action<SanPhamDto, SanPhamBienTheDto?>? SanPhamBienTheSelected;
        public event Action? SanPhamCleared;

        public SanPhamSearchBox() => InitializeComponent();

        public bool IsPopupOpen
        {
            get => Popup.IsOpen;
            set => Popup.IsOpen = value;
        }

        public bool SuppressPopup { get; set; } = false;

        // ================= SELECT =================
        public void SetSelectedSanPham(SanPhamDto sp, SanPhamBienTheDto? bt = null)
        {
            SelectedSanPham = sp;
            SelectedBienThe = bt;

            SearchTextBox.Text = bt != null
                ? $"{sp.Ten} - {bt.TenBienThe} - {bt.GiaBan / 1000:N0}k"
                : sp.Ten;
        }

        public void SetTextWithoutPopup(string text)
        {
            SuppressPopup = true;
            SearchTextBox.Text = text;
            SuppressPopup = false;
        }

        public void SetSelectedSanPhamById(Guid id)
        {
            var sp = SanPhamList.FirstOrDefault(s => s.Id == id);
            if (sp != null)
            {
                var bt = sp.BienThe.FirstOrDefault();
                SetSelectedSanPham(sp, bt);
            }
            else
            {
                Clear();
            }
        }

        public void SetSelectedBienTheById(Guid bienTheId)
        {
            var sp = SanPhamList.FirstOrDefault(s => s.BienThe.Any(bt => bt.Id == bienTheId));
            var bt = sp?.BienThe.FirstOrDefault(x => x.Id == bienTheId);

            if (sp != null && bt != null)
            {
                SetSelectedSanPham(sp, bt);
                return;
            }

            Clear();
        }

        internal void Clear()
        {
            SelectedSanPham = null;
            SelectedBienThe = null;
            SearchTextBox.Text = "";
            Popup.IsOpen = false;
        }

        // ================= UI =================
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Clear();
            ClearButton.Visibility = Visibility.Collapsed;
            SanPhamCleared?.Invoke();
            SearchTextBox.Focus();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearButton.Visibility = string.IsNullOrWhiteSpace(SearchTextBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;

            string keyword = StringHelper.MyNormalizeText(SearchTextBox.Text?.Trim() ?? "");

            if (string.IsNullOrEmpty(keyword))
            {
                ListBoxResults.ItemsSource = null;
                Popup.IsOpen = false;
                return;
            }

            var results = SanPhamList
                .Select(sp =>
                {
                    int score = sp.TimKiemTokens
                        .Where(t => !string.IsNullOrEmpty(t))
                        .Sum(token =>
                            token == keyword ? 500 :
                            token.StartsWith(keyword) ? 300 :
                            token.Contains(keyword) ? 100 : 0);

                    return new { sp, score };
                })
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.sp.ThuTu)
                .Select(x => new SanPhamSearchResult
                {
                    SanPhamId = x.sp.Id,
                    TenSanPham = x.sp.Ten,
                    BienThes = x.sp.BienThe.OrderBy(bt => bt.GiaBan).ToList()
                })
                .Take(20)
                .ToList();

            ListBoxResults.ItemsSource = results;
            Popup.IsOpen = !SuppressPopup && results.Any();
        }

        private void ListBoxResults_MouseLeftButtonUp(object sender, RoutedEventArgs e)
        {
            if (ListBoxResults.SelectedItem is SanPhamSearchResult item)
            {
                var sp = SanPhamList.FirstOrDefault(x => x.Id == item.SanPhamId);
                Select(sp, sp?.BienThe.FirstOrDefault());
            }
        }

        private void BienThe_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SanPhamBienTheDto bt)
            {
                var sp = SanPhamList.FirstOrDefault(x => x.Id == bt.SanPhamId);
                if (sp != null) Select(sp, bt);
            }
        }

        private void Select(SanPhamDto? sp, SanPhamBienTheDto? bt)
        {
            if (sp == null) return;

            SelectedSanPham = sp;
            SelectedBienThe = bt;

            SearchTextBox.Text = bt != null
                ? $"{sp.Ten} - {bt.TenBienThe} - {bt.GiaBan / 1000:N0}k"
                : sp.Ten;

            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            Popup.IsOpen = false;

            SanPhamBienTheSelected?.Invoke(sp, bt);
        }

        // ================= KEYBOARD =================
        private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && Popup.IsOpen && ListBoxResults.Items.Count > 0)
            {
                if (SearchTextBox.IsKeyboardFocusWithin)
                {
                    ListBoxResults.Focus();
                    ListBoxResults.SelectedIndex = 0;
                }
                else if (ListBoxResults.IsKeyboardFocusWithin)
                {
                    int i = ListBoxResults.SelectedIndex;
                    if (i < ListBoxResults.Items.Count - 1)
                        ListBoxResults.SelectedIndex = i + 1;
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Up && ListBoxResults.IsKeyboardFocusWithin)
            {
                int i = ListBoxResults.SelectedIndex;
                if (i > 0) ListBoxResults.SelectedIndex = i - 1;
                else SearchTextBox.Focus();

                e.Handled = true;
            }
            else if (e.Key == Key.Enter && Popup.IsOpen)
            {
                var item = ListBoxResults.SelectedItem as SanPhamSearchResult
                           ?? ListBoxResults.Items[0] as SanPhamSearchResult;

                if (item != null)
                {
                    var sp = SanPhamList.FirstOrDefault(x => x.Id == item.SanPhamId);
                    Select(sp, sp?.BienThe.FirstOrDefault());
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape && Popup.IsOpen)
            {
                Popup.IsOpen = false;
                SearchTextBox.Focus();
                SanPhamCleared?.Invoke();
                e.Handled = true;
            }
        }

        // ================= MOVE =================
        private async void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not Guid spId)
                return;

            var results = ListBoxResults.ItemsSource as List<SanPhamSearchResult>;
            if (results == null) return;

            int index = results.FindIndex(r => r.SanPhamId == spId);
            if (index < 0) return;

            var current = results[index];
            SanPhamSearchResult? neighbor = null;

            if (btn.Name == "UpButton" && index > 0)
                neighbor = results[index - 1];
            else if (btn.Name != "UpButton" && index < results.Count - 1)
                neighbor = results[index + 1];

            if (neighbor == null) return;

            var sp = SanPhamList.FirstOrDefault(x => x.Id == current.SanPhamId);
            var spNeighbor = SanPhamList.FirstOrDefault(x => x.Id == neighbor.SanPhamId);

            if (sp == null || spNeighbor == null) return;

            (sp.ThuTu, spNeighbor.ThuTu) = (spNeighbor.ThuTu, sp.ThuTu);

            sp.LastModified = DateTime.Now;
            spNeighbor.LastModified = DateTime.Now;

            var api = Apis.SanPham;

            var result1 = await api.UpdateAsync(sp.Id, sp);
            var result2 = await api.UpdateAsync(spNeighbor.Id, spNeighbor);

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
                if (sender == SearchTextBox && ListBoxResults.Items.Count > 0 &&
                    ListBoxResults.Items[0] is SanPhamSearchResult item)
                {
                    var sp = SanPhamList.FirstOrDefault(x => x.Id == item.SanPhamId);
                    var bt = sp?.BienThe.FirstOrDefault();
                    Select(sp, bt);
                    e.Handled = true;
                }
                else if (sender == ListBoxResults && ListBoxResults.SelectedItem is SanPhamSearchResult item2)
                {
                    var sp = SanPhamList.FirstOrDefault(x => x.Id == item2.SanPhamId);
                    var bt = sp?.BienThe.FirstOrDefault();
                    Select(sp, bt);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Escape)
            {
                Popup.IsOpen = false;
                SearchTextBox.Focus();
                SanPhamCleared?.Invoke();
                e.Handled = true;
            }
        }
    }
}