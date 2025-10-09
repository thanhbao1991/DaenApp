// SanPhamList.xaml.cs
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
    public partial class SanPhamList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        string _friendlyName = TuDien._tableFriendlyNames["SanPham"];

        public SanPhamList()
        {
            InitializeComponent();
            this.Title = _friendlyName;
            this.TieuDeTextBlock.Text = _friendlyName;
            this.PreviewKeyDown += SanPhamList_PreviewKeyDown;

            while (AppProviders.SanPhams?.Items == null)
            {
                Task.Delay(100); // chờ 100ms rồi kiểm tra lại
            }


            // 1. Gán Source ngay
            _viewSource.Source = AppProviders.SanPhams.Items;
            _viewSource.Filter += ViewSource_Filter;
            SanPhamDataGrid.ItemsSource = _viewSource.View;

            // 2. Subscribe OnChanged (sau khi Source đã có)
            AppProviders.SanPhams.OnChanged += () => ApplySearch();

            // 3. Sau cùng mới reload async
            Loaded += async (_, __) =>
            {
                await AppProviders.SanPhams.ReloadAsync();
                ApplySearch();
            };
        }


        private void ApplySearch()
        {
            _viewSource.View.Refresh();

            // Sort theo LastModified giảm dần
            _viewSource.View.SortDescriptions.Clear();
            _viewSource.View.SortDescriptions.Add(new SortDescription(nameof(SanPhamDto.LastModified), ListSortDirection.Descending));

            // Gán lại STT
            var list = _viewSource.View.Cast<SanPhamDto>().ToList();
            for (int i = 0; i < list.Count; i++)
                list[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not SanPhamDto item)
            {
                e.Accepted = false;
                return;
            }
            var keyword = StringHelper.MyNormalizeText(SearchTextBox.Text.Trim());
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.SanPhams.ReloadAsync();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new SanPhamEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
            };
            if (window.ShowDialog() == true)
                await AppProviders.SanPhams.ReloadAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SanPhamDataGrid.SelectedItem is not SanPhamDto selected)
            {
                MessageBox.Show($"Vui lòng chọn {_friendlyName} cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var window = new SanPhamEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
            };
            if (window.ShowDialog() == true)
                await AppProviders.SanPhams.ReloadAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SanPhamDataGrid.SelectedItem is not SanPhamDto selected)
            {
                MessageBox.Show($"Vui lòng chọn {_friendlyName} cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} '{selected.Ten}'?",
                "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/SanPham/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<SanPhamDto>>();
                if (result?.IsSuccess == true)
                    AppProviders.SanPhams.Remove(selected.Id);
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

        private void SanPhamDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SanPhamDataGrid.SelectedItem is not SanPhamDto selected) return;
            var window = new SanPhamEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight,
                Owner = this
            };
            if (window.ShowDialog() == true)
                _ = AppProviders.SanPhams.ReloadAsync();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void SanPhamList_PreviewKeyDown(object sender, KeyEventArgs e)
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
