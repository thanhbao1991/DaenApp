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

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class NguyenLieuBanHangList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        string _friendlyName = TuDien._tableFriendlyNames["NguyenLieuBanHang"];

        public NguyenLieuBanHangList()
        {
            InitializeComponent();
            this.Title = _friendlyName;
            this.TieuDeTextBlock.Text = _friendlyName;
            this.PreviewKeyDown += NguyenLieuBanHangList_PreviewKeyDown;

            // chờ AppProviders
            while (AppProviders.NguyenLieuBanHangs?.Items == null)
            {
                Task.Delay(100);
            }

            // 1. Gán Source
            _viewSource.Source = AppProviders.NguyenLieuBanHangs.Items;
            _viewSource.Filter += ViewSource_Filter;
            NguyenLieuBanHangDataGrid.ItemsSource = _viewSource.View;

            // 2. Subscribe OnChanged
            AppProviders.NguyenLieuBanHangs.OnChanged += () => ApplySearch();

            // 3. Reload lần đầu
            Loaded += async (_, __) =>
            {
                await AppProviders.NguyenLieuBanHangs.ReloadAsync();
                ApplySearch();
            };
        }

        private void ApplySearch()
        {
            _viewSource.View.Refresh();

            _viewSource.View.SortDescriptions.Clear();
            _viewSource.View.SortDescriptions.Add(
                new SortDescription(nameof(NguyenLieuBanHangDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<NguyenLieuBanHangDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not NguyenLieuBanHangDto item)
            {
                e.Accepted = false;
                return;
            }

            var keyword = StringHelper.MyNormalizeText(SearchTextBox.Text.Trim());
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.NguyenLieuBanHangs.ReloadAsync();
            ApplySearch();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new NguyenLieuBanHangEdit();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
                await AppProviders.NguyenLieuBanHangs.ReloadAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (NguyenLieuBanHangDataGrid.SelectedItem is not NguyenLieuBanHangDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new NguyenLieuBanHangEdit(selected);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
                await AppProviders.NguyenLieuBanHangs.ReloadAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (NguyenLieuBanHangDataGrid.SelectedItem is not NguyenLieuBanHangDto selected)
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
                var response = await ApiClient.DeleteAsync($"/api/NguyenLieuBanHang/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<NguyenLieuBanHangDto>>();
                if (result?.IsSuccess == true)
                    AppProviders.NguyenLieuBanHangs.Remove(selected.Id);
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

        private async void NguyenLieuBanHangDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (NguyenLieuBanHangDataGrid.SelectedItem is not NguyenLieuBanHangDto selected) return;
            var window = new NguyenLieuBanHangEdit(selected);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
                await AppProviders.NguyenLieuBanHangs.ReloadAsync();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => ApplySearch();

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void NguyenLieuBanHangList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            {
                AddButton_Click(null!, null!);
                e.Handled = true;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            {
                EditButton_Click(null!, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                DeleteButton_Click(null!, null!);
                e.Handled = true;
            }
            else if (e.Key == Key.F5)
            {
                ReloadButton_Click(null!, null!);
                e.Handled = true;
            }
        }
    }
}