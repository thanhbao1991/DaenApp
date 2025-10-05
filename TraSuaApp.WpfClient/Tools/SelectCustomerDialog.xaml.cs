using System.Windows;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.WpfClient.Views
{
    public partial class SelectCustomerDialog : Window
    {
        public KhachHangDto? SelectedKhachHang { get; private set; }

        public SelectCustomerDialog()
        {
            InitializeComponent();

            KhachHangBox.KhachHangList = AppProviders.KhachHangs.Items;
            KhachHangBox.ShowAllWhenEmpty = true;
            KhachHangBox.IncludeKhachMoiItem = true;

            // Tự mở popup khi load
            Loaded += (_, __) =>
            {
                KhachHangBox.IsPopupOpen = true;
                KhachHangBox.SearchTextBox.Focus();
            };

            // Double-click → xác nhận chọn
            KhachHangBox.KhachHangConfirmed += kh =>
            {
                SelectedKhachHang = kh;
                DialogResult = true;
                Close();
            };

            // Nút chọn
            KhachHangBox.KhachHangSelected += kh =>
            {
                SelectedKhachHang = kh;
            };

            // Chọn “Khách mới”
            KhachHangBox.KhachMoiSelected += () =>
            {
                SelectedKhachHang = null;
                DialogResult = true;
                Close();
            };
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedKhachHang != null || KhachHangBox.SearchTextBox.Text == "🟟 Khách mới")
            {
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Hãy chọn một khách hàng hoặc 'Khách mới'.");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}