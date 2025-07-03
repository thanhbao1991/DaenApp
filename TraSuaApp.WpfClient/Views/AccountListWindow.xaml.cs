using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class AccountListWindow : Window
    {
        private List<TaiKhoanDto> _allAccounts = new();
        private readonly ErrorHandler _errorHandler = new WpfErrorHandler();

        public AccountListWindow()
        {
            InitializeComponent();
            _ = LoadAccountsAsync();
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadAccountsAsync();
        }

        private async Task LoadAccountsAsync()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var response = await ApiClient.GetAsync("/api/taikhoan");

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<List<TaiKhoanDto>>();
                    if (data != null)
                    {
                        _allAccounts = data;
                        ApplySearch();
                    }
                }
                else
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    _errorHandler.Handle(new Exception($"API lỗi {(int)response.StatusCode}: {msg}"), "Tải tài khoản");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "LoadAccountsAsync");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ApplySearch()
        {
            var keyword = SearchTextBox.Text.Trim().ToLower();
            var filtered = _allAccounts
            .Where(x =>
            x.TenDangNhap.ToLower().Contains(keyword) ||
            (!string.IsNullOrEmpty(x.TenHienThi) && x.TenHienThi.ToLower().Contains(keyword)) ||
            (!string.IsNullOrEmpty(x.VaiTro) && x.VaiTro.ToLower().Contains(keyword)))
            .ToList();

            for (int i = 0; i < filtered.Count; i++)
            {
                filtered[i].STT = i + 1;
            }

            AccountDataGrid.ItemsSource = filtered;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new AccountEditWindow
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };

            if (editWindow.ShowDialog() == true)
            {
                _ = LoadAccountsAsync(); // reload sau khi thêm thành công
            }
        }
        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountDataGrid.SelectedItem is not TaiKhoanDto selected)
            {
                MessageBox.Show("Vui lòng chọn tài khoản cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Lấy dữ liệu từ API để đảm bảo mới nhất
                var response = await ApiClient.GetAsync($"/api/taikhoan/{selected.Id}");
                if (!response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Không thể tải dữ liệu tài khoản: {msg}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var account = await response.Content.ReadFromJsonAsync<TaiKhoanDto>();
                if (account == null)
                {
                    MessageBox.Show("Không tìm thấy dữ liệu tài khoản.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var editWindow = new AccountEditWindow(account)
                {
                    Width = this.ActualWidth,
                    Height = this.ActualHeight
                };

                if (editWindow.ShowDialog() == true)
                {
                    await LoadAccountsAsync(); // ✅ Tải lại danh sách nếu sửa thành công
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "EditButton_Click");
            }
        }
        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountDataGrid.SelectedItem is not TaiKhoanDto selected)
            {
                MessageBox.Show("Vui lòng chọn tài khoản cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // ❌ Không cho xoá chính mình
            if (selected.TenDangNhap == CurrentUser.TenDangNhap)
            {
                MessageBox.Show("Bạn không thể xoá chính mình.", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá tài khoản '{selected.TenDangNhap}'?",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response = await ApiClient.DeleteAsync($"/api/taikhoan/{selected.Id}");

                if (response.IsSuccessStatusCode)
                {
                    //MessageBox.Show("Đã xoá thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadAccountsAsync();
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _errorHandler.Handle(new Exception($"API lỗi {(int)response.StatusCode}: {content}"), "Xoá tài khoản");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "DeleteButton_Click");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void AccountDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Kiểm tra xem phần tử bị double-click có phải là một DataGridRow không
            var row = ItemsControl.ContainerFromElement(AccountDataGrid, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null)
                return; // Không phải double click lên dòng

            // Lấy đối tượng được chọn
            if (AccountDataGrid.SelectedItem is TaiKhoanDto selectedAccount)
            {
                var editWindow = new AccountEditWindow(selectedAccount)
                {
                    Width = this.ActualWidth,
                    Height = this.ActualHeight
                };

                if (editWindow.ShowDialog() == true)
                {
                    _ = LoadAccountsAsync(); // reload danh sách
                }
            }
        }

    }
}