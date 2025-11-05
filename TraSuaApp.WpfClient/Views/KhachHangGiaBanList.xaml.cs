using System.ComponentModel;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.SettingsViews
{
    public partial class KhachHangGiaBanList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["KhachHangGiaBan"];

        public KhachHangGiaBanList()
        {
            InitializeComponent();
            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            PreviewKeyDown += KhachHangGiaBanList_PreviewKeyDown;

            // Gắn source ngay khi khởi tạo
            _viewSource.Source = AppProviders.KhachHangGiaBans.Items;
            _viewSource.Filter += ViewSource_Filter;
            KhachHangGiaBanDataGrid.ItemsSource = _viewSource.View;

            // Subscribe thay đổi
            AppProviders.KhachHangGiaBans.OnChanged += ApplySearch;

            // Reload khi loaded
            Loaded += async (_, __) =>
            {
                await AppProviders.KhachHangGiaBans.ReloadAsync();
                ApplySearch();

                // (khuyến nghị) cũng load caches KH & Biến thể để Edit mở nhanh
                if (AppProviders.KhachHangs != null) await AppProviders.KhachHangs.ReloadAsync();
                if (AppProviders.SanPhamBienThes != null) await AppProviders.SanPhamBienThes.ReloadAsync();
            };
        }

        private void ApplySearch()
        {
            _viewSource.View.Refresh();

            _viewSource.View.SortDescriptions.Clear();
            _viewSource.View.SortDescriptions.Add(
                new SortDescription(nameof(KhachHangGiaBanDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<KhachHangGiaBanDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not KhachHangGiaBanDto item) { e.Accepted = false; return; }

            var keyword = StringHelper.MyNormalizeText((SearchTextBox.Text ?? "").Trim());
            if (string.IsNullOrEmpty(keyword)) { e.Accepted = true; return; }

            var haystack = item.TimKiem ?? "";
            e.Accepted = haystack.Contains(keyword);
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.KhachHangGiaBans.ReloadAsync();
            ApplySearch();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new KhachHangGiaBanEdit
            {
                Width = ActualWidth,
                Height = ActualHeight,
                Owner = this
            };
            if (window.ShowDialog() == true)
                await AppProviders.KhachHangGiaBans.ReloadAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (KhachHangGiaBanDataGrid.SelectedItem is not KhachHangGiaBanDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new KhachHangGiaBanEdit(selected)
            {
                Width = ActualWidth,
                Height = ActualHeight,
                Owner = this
            };
            if (window.ShowDialog() == true)
                await AppProviders.KhachHangGiaBans.ReloadAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (KhachHangGiaBanDataGrid.SelectedItem is not KhachHangGiaBanDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var label = $"{selected.TenKhachHang} - {selected.TenSanPham} / {selected.TenBienThe}";
            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} của '{label}'?",
                "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/KhachHangGiaBan/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<KhachHangGiaBanDto>>();
                if (result?.IsSuccess == true)
                    AppProviders.KhachHangGiaBans.Remove(selected.Id);
                else
                    throw new Exception(result?.Message ?? "Không thể xoá.");
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Delete");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void KhachHangGiaBanDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (KhachHangGiaBanDataGrid.SelectedItem is not KhachHangGiaBanDto selected) return;
            var window = new KhachHangGiaBanEdit(selected) { Owner = this };
            if (window.ShowDialog() == true)
                await AppProviders.KhachHangGiaBans.ReloadAsync();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void KhachHangGiaBanList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N) { AddButton_Click(null!, null!); e.Handled = true; }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E) { EditButton_Click(null!, null!); e.Handled = true; }
            else if (e.Key == Key.Delete) { DeleteButton_Click(null!, null!); e.Handled = true; }
            else if (e.Key == Key.F5) { ReloadButton_Click(null!, null!); e.Handled = true; }
        }
    }
}