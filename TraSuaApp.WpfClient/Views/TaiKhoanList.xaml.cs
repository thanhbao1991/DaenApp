using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class TaiKhoanList : Window
    {
        private List<TaiKhoanDto> _allAccounts = new();
        private readonly ErrorHandler _errorHandler = new WpfErrorHandler();

        public TaiKhoanList()
        {
            InitializeComponent();
            _ = LoadAccountsAsync();
            this.PreviewKeyDown += AccountListWindow_PreviewKeyDown;
        }

        private async Task OpenEditWindowAsync(TaiKhoanDto? account = null)
        {
            var window = new TaiKhoanEdit(account)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };

            if (window.ShowDialog() == true)
                await LoadAccountsAsync();
        }

        private void AccountListWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            {
                AddButton_Click(null!, null!); e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            {
                EditButton_Click(null!, null!); e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                DeleteButton_Click(null!, null!); e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                ReloadButton_Click(null!, null!); e.Handled = true;
            }
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
                if (!response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API lỗi {(int)response.StatusCode}: {msg}");
                }

                var result = await response.Content.ReadFromJsonAsync<Result<List<TaiKhoanDto>>>();
                if (result?.IsSuccess != true || result.Data == null)
                    throw new Exception(result?.Message ?? "Không thể tải danh sách tài khoản.");

                _allAccounts = result.Data.OrderBy(x => x.ThoiGianTao).ToList();
                foreach (var a in _allAccounts)
                    a.TenNormalized = TextSearchHelper.NormalizeText(a.TenHienThi ?? "");

                ApplySearch();
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Tải tài khoản");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ApplySearch()
        {
            var keyword = SearchTextBox.Text.Trim();
            var filtered = TextSearchHelper.FilterTaiKhoans(_allAccounts, keyword);

            for (int i = 0; i < filtered.Count; i++)
                filtered[i].STT = i + 1;

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

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenEditWindowAsync();
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
                var response = await ApiClient.GetAsync($"/api/taikhoan/{selected.Id}");
                if (!response.IsSuccessStatusCode)
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API lỗi {(int)response.StatusCode}: {msg}");
                }

                var result = await response.Content.ReadFromJsonAsync<Result<TaiKhoanDto>>();
                if (result?.IsSuccess != true || result.Data == null)
                    throw new Exception(result?.Message ?? "Không tìm thấy tài khoản.");

                await OpenEditWindowAsync(result.Data);
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Sửa tài khoản");
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (AccountDataGrid.SelectedItem is not TaiKhoanDto selected)
            {
                MessageBox.Show("Vui lòng chọn tài khoản cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                _errorHandler.Handle(ex, "Xoá tài khoản");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void AccountDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = ItemsControl.ContainerFromElement(AccountDataGrid, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null) return;

            if (row.Item is TaiKhoanDto selected)
            {
                try
                {
                    var response = await ApiClient.GetAsync($"/api/taikhoan/{selected.Id}");
                    if (!response.IsSuccessStatusCode) return;

                    var result = await response.Content.ReadFromJsonAsync<Result<TaiKhoanDto>>();
                    if (result?.IsSuccess != true || result.Data == null) return;

                    await OpenEditWindowAsync(result.Data);
                }
                catch (Exception ex)
                {
                    _errorHandler.Handle(ex, "Mở tài khoản");
                }
            }
        }
    }
}