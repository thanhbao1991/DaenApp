using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Controls
{
    public partial class SanPhamSearchBox : UserControl
    {
        public List<SanPhamDto> SanPhamList { get; set; } = new();
        public SanPhamDto? SelectedSanPham { get; private set; }
        public event Action<SanPhamDto>? SanPhamSelected;
        public event Action? SanPhamCleared;

        public SanPhamSearchBox()
        {
            InitializeComponent();
        }

        public bool IsPopupOpen => ListBoxResults.Items.Count > 0;
        public bool SuppressPopup { get; set; } = false;

        public void SetSelectedSanPham(SanPhamDto sp)
        {
            SelectedSanPham = sp;
            SearchTextBox.Text = sp.Ten;
        }

        public void SetTextWithoutPopup(string text)
        {
            SuppressPopup = true;
            SearchTextBox.Text = text;
            SuppressPopup = false;
        }

        public void SetSelectedSanPhamByIdWithoutPopup(Guid id)
        {
            SuppressPopup = true;
            SetSelectedSanPhamById(id);
            SuppressPopup = false;
        }

        public void SetSelectedSanPhamById(Guid id)
        {
            var sp = SanPhamList.FirstOrDefault(x => x.Id == id);
            if (sp != null)
                SetSelectedSanPham(sp);
            else
            {
                SelectedSanPham = null;
                SearchTextBox.Text = "";
                Popup.IsOpen = false;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            SelectedSanPham = null;
            ClearButton.Visibility = Visibility.Collapsed;
            SanPhamCleared?.Invoke();
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

            var result = SanPhamList
                .Where(sp => (sp.TimKiem ?? "").Contains(keyword) || parts.All(p => (sp.TimKiem ?? "").Contains(p)))
                .Take(50)
                .OrderBy(x => x.Ten)
                .ToList();

            ListBoxResults.ItemsSource = result;
            Popup.IsOpen = !SuppressPopup && result.Any();
        }

        private void ListBoxResults_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ListBoxResults.SelectedItem is SanPhamDto sp)
                Select(sp);
            SearchTextBox.Focus();
            SearchTextBox.SelectAll();
        }

        private void Select(SanPhamDto sp)
        {
            SelectedSanPham = sp;
            SearchTextBox.Text = sp.Ten;
            SearchTextBox.CaretIndex = SearchTextBox.Text.Length;
            Popup.IsOpen = false;
            SanPhamSelected?.Invoke(sp);
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
                SanPhamDto? sp = null;
                if (sender == SearchTextBox && ListBoxResults.Items.Count > 0)
                    sp = ListBoxResults.Items[0] as SanPhamDto;
                else if (sender == ListBoxResults && ListBoxResults.SelectedItem is SanPhamDto selected)
                    sp = selected;

                if (sp != null)
                {
                    Select(sp);
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
