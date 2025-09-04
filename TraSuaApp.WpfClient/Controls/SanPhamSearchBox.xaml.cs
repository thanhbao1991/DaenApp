using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
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

        public void SetSelectedSanPham(SanPhamDto sp, SanPhamBienTheDto? bt = null)
        {
            SelectedSanPham = sp;
            SelectedBienThe = bt;
            SearchTextBox.Text = bt != null
                ? $"{sp.Ten} - {bt.TenBienThe} - {bt.GiaBan:n0} đ"
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
                SelectedSanPham = null;
                SelectedBienThe = null;
                SearchTextBox.Text = "";
                Popup.IsOpen = false;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            SelectedSanPham = null;
            SelectedBienThe = null;
            ClearButton.Visibility = Visibility.Collapsed;
            SanPhamCleared?.Invoke();
            SearchTextBox.Focus();
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

            var results = SanPhamList
                .Select(sp =>
                {
                    int score = 0;

                    foreach (var token in sp.TimKiemTokens.Where(t => !string.IsNullOrEmpty(t)))
                    {
                        if (token == keyword) score += 500;
                        else if (token.StartsWith(keyword)) score += 300;
                        else if (token.Contains(keyword)) score += 100;
                    }

                    return new { sp, score };
                })
                .Where(x => x.score > 0)
                .OrderByDescending(x => x.sp.DaBan)

                .Select(x => new SanPhamSearchResult
                {
                    SanPhamId = x.sp.Id,
                    TenSanPham = x.sp.Ten,
                    BienThes = x.sp.BienThe
        .OrderBy(bt => bt.GiaBan) // sắp xếp biến thể theo giá bán
        .ToList()
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
                var bt = sp?.BienThe.FirstOrDefault();
                Select(sp, bt);
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
                ? $"{sp.Ten} - {bt.TenBienThe} - {bt.GiaBan:n0} đ"
                : sp.Ten;

            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            Popup.IsOpen = false;

            SanPhamBienTheSelected?.Invoke(sp, bt);
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
            //    if (sender == SearchTextBox && ListBoxResults.Items.Count > 0 &&
            //        ListBoxResults.Items[0] is SanPhamSearchResult item)
            //    {
            //        var sp = SanPhamList.FirstOrDefault(x => x.Id == item.SanPhamId);
            //        var bt = sp?.BienThe.FirstOrDefault();
            //        Select(sp, bt);
            //        e.Handled = true;
            //    }
            //    else if (sender == ListBoxResults && ListBoxResults.SelectedItem is SanPhamSearchResult item2)
            //    {
            //        var sp = SanPhamList.FirstOrDefault(x => x.Id == item2.SanPhamId);
            //        var bt = sp?.BienThe.FirstOrDefault();
            //        Select(sp, bt);
            //        e.Handled = true;
            //    }
            //}
            //else if (e.Key == Key.Escape)
            //{
            //    Popup.IsOpen = false;
            //    SearchTextBox.Focus();
            //    SanPhamCleared?.Invoke();
            //    e.Handled = true;
            //}
        }




        private void Root_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && Popup.IsOpen && ListBoxResults.Items.Count > 0)
            {
                if (SearchTextBox.IsKeyboardFocusWithin)
                {
                    // từ textbox → nhảy vào listbox dòng đầu
                    ListBoxResults.Focus();
                    ListBoxResults.SelectedIndex = 0;
                    e.Handled = true;
                }
                else if (ListBoxResults.IsKeyboardFocusWithin)
                {
                    // đang ở listbox → di chuyển xuống
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
                    // di chuyển lên
                    int index = ListBoxResults.SelectedIndex;
                    if (index > 0)
                    {
                        ListBoxResults.SelectedIndex = index - 1;
                        ListBoxResults.ScrollIntoView(ListBoxResults.SelectedItem);
                        e.Handled = true;
                    }
                    else
                    {
                        // lên tới đầu → quay lại textbox
                        SearchTextBox.Focus();
                        e.Handled = true;
                    }
                }
            }
            else if (e.Key == Key.Enter)
            {
                if (Popup.IsOpen)
                {
                    SanPhamSearchResult? item = null;

                    if (ListBoxResults.SelectedItem is SanPhamSearchResult selected)
                        item = selected;
                    else if (ListBoxResults.Items.Count > 0)
                        item = ListBoxResults.Items[0] as SanPhamSearchResult;

                    if (item != null)
                    {
                        var sp = SanPhamList.FirstOrDefault(x => x.Id == item.SanPhamId);
                        var bt = sp?.BienThe.FirstOrDefault();

                        if (sp != null)
                        {
                            Select(sp, bt);
                            Popup.IsOpen = false;
                            e.Handled = true;
                        }
                    }
                }
            }
            else if (e.Key == Key.Escape)
            {
                if (Popup.IsOpen)
                {
                    Popup.IsOpen = false;
                    SearchTextBox.Focus();
                    SanPhamCleared?.Invoke();
                    e.Handled = true;
                }
            }
        }


        private async void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Guid spId)
            {
                // Lấy kết quả hiện đang hiển thị
                var results = ListBoxResults.ItemsSource as List<SanPhamSearchResult>;
                if (results == null) return;

                // Tìm sản phẩm đang bấm trong danh sách lọc
                var current = results.FirstOrDefault(r => r.SanPhamId == spId);
                if (current == null) return;

                int index = results.FindIndex(r => r.SanPhamId == spId);
                SanPhamSearchResult? neighbor = null;

                if (btn.Name == "UpButton" && index > 0)
                {
                    neighbor = results[index - 1]; // dòng phía trên trong danh sách lọc
                }
                else if (btn.Name != "UpButton" && index < results.Count - 1)
                {
                    neighbor = results[index + 1]; // dòng phía dưới trong danh sách lọc
                }

                if (neighbor != null)
                {
                    var sp = SanPhamList.FirstOrDefault(x => x.Id == current.SanPhamId);
                    var spNeighbor = SanPhamList.FirstOrDefault(x => x.Id == neighbor.SanPhamId);
                    if (sp == null || spNeighbor == null) return;

                    // Hoán đổi DaBan
                    int temp = sp.DaBan;
                    sp.DaBan = spNeighbor.DaBan;
                    spNeighbor.DaBan = temp;

                    sp.LastModified = DateTime.Now;
                    spNeighbor.LastModified = DateTime.Now;

                    var api = new SanPhamApi();
                    var result1 = await api.UpdateSingleAsync(sp.Id, sp);
                    var result2 = await api.UpdateSingleAsync(spNeighbor.Id, spNeighbor);

                    if (result1.IsSuccess && result2.IsSuccess)
                    {
                        // Load lại danh sách
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