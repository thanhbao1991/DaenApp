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
    public partial class TuDienTraCuuList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        string _friendlyName = TuDien._tableFriendlyNames["TuDienTraCuu"];

        public TuDienTraCuuList()
        {
            InitializeComponent();
            this.Title = _friendlyName;
            this.TieuDeTextBlock.Text = _friendlyName;
            this.PreviewKeyDown += TuDienTraCuuList_PreviewKeyDown;

            while (AppProviders.TuDienTraCuus?.Items == null)
            {
                Task.Delay(100); // chờ 100ms rồi kiểm tra lại
            }


            // 1. Gán Source ngay
            _viewSource.Source = AppProviders.TuDienTraCuus.Items;
            _viewSource.Filter += ViewSource_Filter;

            TuDienTraCuuDataGrid.ItemsSource = _viewSource.View;

            // 2. Subscribe OnChanged (sau khi Source đã có)
            AppProviders.TuDienTraCuus.OnChanged += () => ApplySearch();

            // 3. Sau cùng mới reload async
            Loaded += async (_, __) =>
            {
                await AppProviders.TuDienTraCuus.ReloadAsync();
                ApplySearch();
            };
        }
        private void ApplySearch()
        {
            _viewSource.View.Refresh();

            _viewSource.View.SortDescriptions.Clear();
            _viewSource.View.SortDescriptions.Add(new SortDescription(nameof(TuDienTraCuuDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<TuDienTraCuuDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not TuDienTraCuuDto item)
            {
                e.Accepted = false;
                return;
            }

            var keyword = StringHelper.MyNormalizeText(SearchTextBox.Text.Trim());
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }
        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.TuDienTraCuus.ReloadAsync();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new TuDienTraCuuEdit();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
                await AppProviders.TuDienTraCuus.ReloadAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (TuDienTraCuuDataGrid.SelectedItem is not TuDienTraCuuDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new TuDienTraCuuEdit(selected);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
                await AppProviders.TuDienTraCuus.ReloadAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TuDienTraCuuDataGrid.SelectedItem is not TuDienTraCuuDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} '{selected.Ten}'?",
                "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/TuDienTraCuu/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<TuDienTraCuuDto>>();
                if (result?.IsSuccess == true)
                    AppProviders.TuDienTraCuus.Remove(selected.Id);
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

        private async void TuDienTraCuuDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TuDienTraCuuDataGrid.SelectedItem is not TuDienTraCuuDto selected) return;
            var window = new TuDienTraCuuEdit(selected);
            if (window.ShowDialog() == true)
                await AppProviders.TuDienTraCuus.ReloadAsync();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void TuDienTraCuuList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N) { AddButton_Click(null!, null!); e.Handled = true; }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E) { EditButton_Click(null!, null!); e.Handled = true; }
            else if (e.Key == Key.Delete) { DeleteButton_Click(null!, null!); e.Handled = true; }
            else if (e.Key == Key.F5) { ReloadButton_Click(null!, null!); e.Handled = true; }
        }


    }
}
