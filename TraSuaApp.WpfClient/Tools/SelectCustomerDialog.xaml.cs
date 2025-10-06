using System.Windows;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.WpfClient.Views
{
    public partial class SelectCustomerDialog : Window
    {
        public KhachHangDto? SelectedKhachHang { get; private set; }

        // 🟟 Cờ báo người dùng chọn “Khách mới”
        public bool RequestedNewCustomer { get; private set; }

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

            // Double-click / Enter → xác nhận chọn
            KhachHangBox.KhachHangConfirmed += kh =>
            {
                SelectedKhachHang = kh;
                CloseAsDialog(true);
            };

            // Chọn trong list nhưng chưa xác nhận
            KhachHangBox.KhachHangSelected += kh =>
            {
                SelectedKhachHang = kh;
            };

            // Chọn “Khách mới”
            KhachHangBox.KhachMoiSelected += () =>
            {
                RequestedNewCustomer = true;
                SelectedKhachHang = null;
                // KHÔNG gán DialogResult trực tiếp → tránh crash
                CloseAsDialog(false);
            };
        }

        // Nếu bạn có nút “Xác nhận”
        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedKhachHang != null)
            {
                CloseAsDialog(true);
            }
            else if (RequestedNewCustomer)
            {
                // Người dùng đã chọn “Khách mới”
                CloseAsDialog(false);
            }
            else
            {
                MessageBox.Show("Hãy chọn một khách hàng hoặc 'Khách mới'.");
            }
        }

        // Nếu bạn có nút “Huỷ”
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            CloseAsDialog(false);
        }

        // 🟟 Đóng dialog an toàn (không crash nếu không phải ShowDialog)
        private void CloseAsDialog(bool? result)
        {
            try
            {
                this.DialogResult = result; // chỉ hợp lệ khi mở bằng ShowDialog()
            }
            catch
            {
                // Nếu window không phải dialog (hoặc host đặc thù), fallback
            }
            finally
            {
                this.Close();
            }
        }
    }
}