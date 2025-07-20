using System.ComponentModel;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class KhachHangList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        string _friendlyName = TuDien._tableFriendlyNames["KhachHang"];

        public KhachHangList()
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _viewSource.Source = AppProviders.KhachHangs.Items;
            _viewSource.Filter += ViewSource_Filter;
            KhachHangDataGrid.ItemsSource = _viewSource.View;

            this.PreviewKeyDown += KhachHangListWindow_PreviewKeyDown;

            // Tự cập nhật khi có thay đổi
            AppProviders.KhachHangs.OnChanged += ApplySearch;

            _ = AppProviders.KhachHangs.ReloadAsync();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            //0throw new NotImplementedException();
        }

        private void ApplySearch()
        {
            _viewSource.View.Refresh();

            _viewSource.View.SortDescriptions.Clear();
            _viewSource.View.SortDescriptions.Add(new SortDescription(nameof(KhachHangDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<KhachHangDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].STT = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not KhachHangDto item)
            {
                e.Accepted = false;
                return;
            }

            var keyword = TextSearchHelper.NormalizeText(SearchTextBox.Text.Trim());
            if (string.IsNullOrEmpty(keyword))
            {
                e.Accepted = true;
                return;
            }

            bool match =
                (!string.IsNullOrEmpty(item.TimKiem) && item.TimKiem.Contains(keyword));

            e.Accepted = match;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.KhachHangs.ReloadAsync();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new KhachHangEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };
            if (window.ShowDialog() == true)
                await AppProviders.KhachHangs.ReloadAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (KhachHangDataGrid.SelectedItem is not KhachHangDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new KhachHangEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };
            if (window.ShowDialog() == true)
                await AppProviders.KhachHangs.ReloadAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (KhachHangDataGrid.SelectedItem is not KhachHangDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} '{selected.Ten}'?",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response = await ApiClient.DeleteAsync($"/api/khachhang/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<KhachHangDto>>();
                if (result?.IsSuccess == true)
                {
                    AppProviders.KhachHangs.Remove(selected.Id);
                }
                else
                {
                    throw new Exception(result?.Message ?? $"Không thể xoá {_friendlyName}.");
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

        private void KhachHangDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = ItemsControl.ContainerFromElement(KhachHangDataGrid, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row?.Item is not KhachHangDto selected) return;

            EditButton_Click(null!, null!);
        }

        private void KhachHangListWindow_PreviewKeyDown(object sender, KeyEventArgs e)
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
    }
}