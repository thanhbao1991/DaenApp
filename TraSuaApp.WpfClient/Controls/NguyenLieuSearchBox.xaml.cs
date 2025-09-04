using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class NguyenLieuSearchBox : UserControl
    {
        public List<NguyenLieuDto> NguyenLieuList { get; set; } = new();
        public NguyenLieuDto? SelectedNguyenLieu { get; private set; }
        public event Action<NguyenLieuDto>? NguyenLieuSelected;
        public event Action? NguyenLieuCleared;

        public NguyenLieuSearchBox()
        {
            InitializeComponent();
        }
        private Guid _value = Guid.Empty;

        public void SetSelectedNguyenLieu(NguyenLieuDto sp)
        {
            SelectedNguyenLieu = sp;
            SearchTextBox.Text = sp.Ten;
        }

        public bool IsPopupOpen
        {
            get => Popup.IsOpen;
            set => Popup.IsOpen = value;
        }

        public void SetTextWithoutPopup(string text)
        {
            SuppressPopup = true;
            SearchTextBox.Text = text;
            SuppressPopup = false;
        }
        public bool SuppressPopup { get; set; } = false;
        public void SetSelectedNguyenLieuByIdWithoutPopup(Guid id)
        {
            SuppressPopup = true;
            SetSelectedNguyenLieuById(id);
            SuppressPopup = false;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            SelectedNguyenLieu = null;
            ClearButton.Visibility = Visibility.Collapsed;
            NguyenLieuCleared?.Invoke();
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

            var result = NguyenLieuList
                .Where(sp => (sp.TimKiem ?? "").Contains(keyword)
                     || parts.All(p => (sp.TimKiem ?? "").Contains(p))
                )
                .Take(20)
                .OrderBy(x => x.Ten)
                .ToList();

            ListBoxResults.ItemsSource = result;

            // 🟟 Chỉ mở Popup nếu không bị suppress
            Popup.IsOpen = !SuppressPopup && result.Any();
        }
        private void ListBoxResults_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ListBoxResults.SelectedItem is NguyenLieuDto sp)
                Select(sp);
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }

        private void Select(NguyenLieuDto sp)
        {
            SelectedNguyenLieu = sp;
            SearchTextBox.Text = sp.Ten;
            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            Popup.IsOpen = false;
            NguyenLieuSelected?.Invoke(sp);
        }

        private void SearchBox_And_ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //// Nếu đang ở TextBox và nhấn phím xuống
            //if (sender == SearchTextBox && e.Key == Key.Down && ListBoxResults.Items.Count > 0)
            //{
            //    ListBoxResults.Focus();
            //    ListBoxResults.SelectedIndex = 0;
            //    e.Handled = true;
            //}
            //// Nếu nhấn Enter
            //else if (e.Key == Key.Enter)
            //{
            //    NguyenLieuDto? sp = null;

            //    if (sender == SearchTextBox && ListBoxResults.Items.Count > 0)
            //    {
            //        // Lấy kết quả đầu tiên
            //        sp = ListBoxResults.Items[0] as NguyenLieuDto;
            //    }
            //    else if (sender == ListBoxResults && ListBoxResults.SelectedItem is NguyenLieuDto selected)
            //    {
            //        // Lấy mục đang chọn
            //        sp = selected;
            //    }

            //    if (sp != null)
            //    {
            //        Select(sp);
            //        e.Handled = true;
            //    }
            //}
            //// Nếu nhấn Escape
            //else if (e.Key == Key.Escape)
            //{
            //    Popup.IsOpen = false;
            //    SearchTextBox.Focus();
            //    NguyenLieuCleared?.Invoke();
            //    e.Handled = true;
            //}
        }

        public void SetSelectedNguyenLieuById(Guid id)
        {
            var nguyenLieu = NguyenLieuList.FirstOrDefault(x => x.Id == id);
            if (nguyenLieu != null)
                SetSelectedNguyenLieu(nguyenLieu);
            else
            {
                SelectedNguyenLieu = null;
                SearchTextBox.Text = "";
                Popup.IsOpen = false;
            }
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
                    NguyenLieuDto? item = null;

                    if (ListBoxResults.SelectedItem is NguyenLieuDto selected)
                        item = selected;
                    else if (ListBoxResults.Items.Count > 0)
                        item = ListBoxResults.Items[0] as NguyenLieuDto;

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
                    NguyenLieuCleared?.Invoke();
                    e.Handled = true;
                }
            }
        }
    }
}
