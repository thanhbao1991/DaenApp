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
    public partial class CongThucList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["CongThuc"];

        public CongThucList()
        {
            InitializeComponent();
            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            PreviewKeyDown += CongThucList_PreviewKeyDown;

            // Chờ AppProviders.CongThucs được khởi tạo
            while (AppProviders.CongThucs?.Items == null)
            {
                Task.Delay(100);
            }

            _viewSource.Source = AppProviders.CongThucs.Items;
            _viewSource.Filter += ViewSource_Filter;
            CongThucDataGrid.ItemsSource = _viewSource.View;

            AppProviders.CongThucs.OnChanged += () => ApplySearch();

            Loaded += async (_, __) =>
            {
                await AppProviders.CongThucs.ReloadAsync();
                ApplySearch();
            };
        }

        #region Filter + Sort + STT

        private void ApplySearch()
        {
            _viewSource.View.Refresh();

            _viewSource.View.SortDescriptions.Clear();
            _viewSource.View.SortDescriptions.Add(
                new SortDescription(nameof(CongThucDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<CongThucDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not CongThucDto item)
            {
                e.Accepted = false;
                return;
            }

            var keyword = StringHelper.MyNormalizeText(SearchTextBox.Text.Trim());
            if (string.IsNullOrEmpty(keyword))
            {
                e.Accepted = true;
                return;
            }

            var timKiem = item.TimKiem ?? "";
            e.Accepted = timKiem.Contains(keyword);
        }

        #endregion

        #region Buttons

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.CongThucs.ReloadAsync();
            ApplySearch();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new CongThucEdit();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
                await AppProviders.CongThucs.ReloadAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (CongThucDataGrid.SelectedItem is not CongThucDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new CongThucEdit(selected);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
                await AppProviders.CongThucs.ReloadAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (CongThucDataGrid.SelectedItem is not CongThucDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} cho '{selected.TenSanPham} - {selected.TenBienThe}'?",
                "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/CongThuc/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<CongThucDto>>();
                if (result?.IsSuccess == true)
                    AppProviders.CongThucs.Remove(selected.Id);
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

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        #endregion

        #region DataGrid events

        private async void CongThucDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CongThucDataGrid.SelectedItem is not CongThucDto selected) return;

            var window = new CongThucEdit(selected);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
                await AppProviders.CongThucs.ReloadAsync();
        }

        /// <summary>
        /// Mở form Nguyên liệu cho công thức đang chọn
        /// </summary>
        private void NguyenLieuButton_Click(object sender, RoutedEventArgs e)
        {
            if (CongThucDataGrid.SelectedItem is not CongThucDto selected)
            {
                MessageBox.Show("Vui lòng chọn công thức.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Mở SuDungNguyenLieuList với công thức đã chọn
            var window = new SuDungNguyenLieuList(selected);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        #endregion

        #region Search + Hotkeys

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

        private void CongThucList_PreviewKeyDown(object sender, KeyEventArgs e)
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

        #endregion
    }
}