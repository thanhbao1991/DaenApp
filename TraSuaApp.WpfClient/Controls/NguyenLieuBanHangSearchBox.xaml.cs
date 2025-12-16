using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class NguyenLieuBanHangSearchBox : UserControl
    {
        public List<NguyenLieuBanHangDto> NguyenLieuBanHangList { get; set; } = new();
        public NguyenLieuBanHangDto? SelectedNguyenLieuBanHang { get; private set; }

        public event Action<NguyenLieuBanHangDto>? NguyenLieuBanHangSelected;
        public event Action? NguyenLieuBanHangCleared;

        public NguyenLieuBanHangSearchBox()
        {
            InitializeComponent();
        }

        public bool SuppressPopup { get; set; } = false;

        public bool IsPopupOpen
        {
            get => Popup.IsOpen;
            set => Popup.IsOpen = value;
        }

        public void SetSelectedNguyenLieuBanHang(NguyenLieuBanHangDto dto)
        {
            SelectedNguyenLieuBanHang = dto;
            SearchTextBox.Text = dto.TenPhienDich;
        }

        public void SetSelectedNguyenLieuBanHangById(Guid id)
        {
            var item = NguyenLieuBanHangList.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                SetSelectedNguyenLieuBanHang(item);
            }
            else
            {
                SelectedNguyenLieuBanHang = null;
                SearchTextBox.Text = "";
                Popup.IsOpen = false;
            }
        }

        public void SetSelectedNguyenLieuBanHangByIdWithoutPopup(Guid id)
        {
            SuppressPopup = true;
            SetSelectedNguyenLieuBanHangById(id);
            SuppressPopup = false;
        }

        public void SetTextWithoutPopup(string text)
        {
            SuppressPopup = true;
            SearchTextBox.Text = text;
            SuppressPopup = false;
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            SelectedNguyenLieuBanHang = null;
            ClearButton.Visibility = Visibility.Collapsed;
            NguyenLieuBanHangCleared?.Invoke();
            SearchTextBox.Focus();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ClearButton.Visibility = string.IsNullOrWhiteSpace(SearchTextBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;

            string keyword = StringHelper.MyNormalizeText(SearchTextBox.Text.Trim());
            if (string.IsNullOrEmpty(keyword))
            {
                ListBoxResults.ItemsSource = null;
                Popup.IsOpen = false;
                return;
            }

            var parts = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var result = NguyenLieuBanHangList
                .Where(x => x.DangSuDung) // chỉ chọn item đang sử dụng
                .Where(nl =>
                {
                    var timKiem = nl.TimKiem ?? "";
                    return timKiem.Contains(keyword) ||
                           parts.All(p => timKiem.Contains(p));
                })
                .Take(20)
                .OrderBy(x => x.TenPhienDich)
                .ToList();

            ListBoxResults.ItemsSource = result;
            Popup.IsOpen = !SuppressPopup && result.Any();
        }

        private void ListBoxResults_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ListBoxResults.SelectedItem is NguyenLieuBanHangDto dto)
                Select(dto);

            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }

        private void Select(NguyenLieuBanHangDto dto)
        {
            SelectedNguyenLieuBanHang = dto;
            SearchTextBox.Text = dto.TenPhienDich;
            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            Popup.IsOpen = false;
            NguyenLieuBanHangSelected?.Invoke(dto);
        }

        private void SearchBox_And_ListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // để trống – dùng Root_PreviewKeyDown xử lý phím
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
                    NguyenLieuBanHangDto? item = null;

                    if (ListBoxResults.SelectedItem is NguyenLieuBanHangDto selected)
                        item = selected;
                    else if (ListBoxResults.Items.Count > 0)
                        item = ListBoxResults.Items[0] as NguyenLieuBanHangDto;

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
                    NguyenLieuBanHangCleared?.Invoke();
                    e.Handled = true;
                }
            }
        }
    }
}