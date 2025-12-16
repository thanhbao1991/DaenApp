using System.ComponentModel;
using System.Globalization;
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
    public partial class NguyenLieuList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["NguyenLieu"];

        public NguyenLieuList()
        {
            InitializeComponent();

            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            PreviewKeyDown += NguyenLieuList_PreviewKeyDown;

            while (AppProviders.NguyenLieus?.Items == null)
            {
                Task.Delay(100);
            }

            // 1) source
            _viewSource.Source = AppProviders.NguyenLieus.Items;
            _viewSource.Filter += ViewSource_Filter;
            NguyenLieuDataGrid.ItemsSource = _viewSource.View;

            // 2) reload + refresh
            AppProviders.NguyenLieus.OnChanged += () => ApplySearch();

            // ✅ Khi NL bán hàng đổi, list NL cũng cần Refresh vì cột lookup phụ thuộc
            if (AppProviders.NguyenLieuBanHangs != null)
                AppProviders.NguyenLieuBanHangs.OnChanged += () => ApplySearch();

            Loaded += async (_, __) =>
            {
                await AppProviders.NguyenLieus.ReloadAsync();
                ApplySearch();
            };
        }

        private void ApplySearch()
        {
            _viewSource.View.Refresh();

            _viewSource.View.SortDescriptions.Clear();
            _viewSource.View.SortDescriptions.Add(
                new SortDescription(nameof(NguyenLieuDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<NguyenLieuDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not NguyenLieuDto item)
            {
                e.Accepted = false;
                return;
            }

            var keyword = StringHelper.MyNormalizeText(SearchTextBox.Text.Trim());
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.NguyenLieus.ReloadAsync();
            ApplySearch();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new NguyenLieuEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
            };

            if (window.ShowDialog() == true)
                await AppProviders.NguyenLieus.ReloadAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (NguyenLieuDataGrid.SelectedItem is not NguyenLieuDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new NguyenLieuEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
            };

            if (window.ShowDialog() == true)
                await AppProviders.NguyenLieus.ReloadAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (NguyenLieuDataGrid.SelectedItem is not NguyenLieuDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} '{selected.Ten}'?",
                "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/NguyenLieu/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<NguyenLieuDto>>();

                if (result?.IsSuccess == true)
                    AppProviders.NguyenLieus.Remove(selected.Id);
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

        private async void NguyenLieuDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (NguyenLieuDataGrid.SelectedItem is not NguyenLieuDto selected) return;

            var window = new NguyenLieuEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
            };

            if (window.ShowDialog() == true)
                await AppProviders.NguyenLieus.ReloadAsync();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void NguyenLieuList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N) { AddButton_Click(null!, null!); e.Handled = true; }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E) { EditButton_Click(null!, null!); e.Handled = true; }
            else if (e.Key == Key.Delete) { DeleteButton_Click(null!, null!); e.Handled = true; }
            else if (e.Key == Key.F5) { ReloadButton_Click(null!, null!); e.Handled = true; }
        }
    }

    // ==========================================================
    // Converters lookup theo NguyenLieuBanHangId (để khỏi sửa DTO)
    // ==========================================================

    public class NguyenLieuBanHangNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Guid id || id == Guid.Empty) return "";
            var list = AppProviders.NguyenLieuBanHangs?.Items;
            var item = list?.FirstOrDefault(x => x.Id == id);
            return item?.TenPhienDich ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class NguyenLieuBanHangDonViConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Guid id || id == Guid.Empty) return "";
            var list = AppProviders.NguyenLieuBanHangs?.Items;
            var item = list?.FirstOrDefault(x => x.Id == id);
            return item?.DonViTinh ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}