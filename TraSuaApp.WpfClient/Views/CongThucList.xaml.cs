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
    public partial class CongThucMasterDetailList : Window
    {
        private readonly CollectionViewSource _congThucViewSource = new();
        private readonly CollectionViewSource _suDungViewSource = new();

        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["CongThuc"];

        private CongThucDto? _selectedCongThuc;

        public CongThucMasterDetailList()
        {
            InitializeComponent();

            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            PreviewKeyDown += CongThucMasterDetail_PreviewKeyDown;

            // Chờ AppProviders (giữ đúng pattern hiện tại của anh)
            while (AppProviders.CongThucs?.Items == null || AppProviders.SuDungNguyenLieus?.Items == null)
            {
                Task.Delay(100);
            }

            // Master source
            _congThucViewSource.Source = AppProviders.CongThucs.Items;
            _congThucViewSource.Filter += CongThucViewSource_Filter;
            CongThucDataGrid.ItemsSource = _congThucViewSource.View;

            AppProviders.CongThucs.OnChanged += () => ApplyCongThucSearch();

            // Detail source
            _suDungViewSource.Source = AppProviders.SuDungNguyenLieus.Items;
            _suDungViewSource.Filter += SuDungViewSource_Filter;
            SuDungNguyenLieuDataGrid.ItemsSource = _suDungViewSource.View;

            AppProviders.SuDungNguyenLieus.OnChanged += () => ApplySuDungSearch();

            Loaded += async (_, __) =>
            {
                await AppProviders.CongThucs.ReloadAsync();
                await AppProviders.SuDungNguyenLieus.ReloadAsync();

                ApplyCongThucSearch();

                // auto select dòng đầu nếu có
                if (CongThucDataGrid.Items.Count > 0)
                {
                    CongThucDataGrid.SelectedIndex = 0;
                }

                RefreshDetailTitle();
                ToggleDetailButtons();
                ApplySuDungSearch();
            };
        }

        // =========================
        // MASTER: FILTER + SORT + STT
        // =========================
        private void ApplyCongThucSearch()
        {
            _congThucViewSource.View.Refresh();

            _congThucViewSource.View.SortDescriptions.Clear();
            _congThucViewSource.View.SortDescriptions.Add(
                new SortDescription(nameof(CongThucDto.LastModified), ListSortDirection.Descending));

            var view = _congThucViewSource.View.Cast<CongThucDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void CongThucViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not CongThucDto item)
            {
                e.Accepted = false;
                return;
            }

            var keyword = StringHelper.MyNormalizeText(CongThucSearchTextBox.Text.Trim());
            if (string.IsNullOrEmpty(keyword))
            {
                e.Accepted = true;
                return;
            }

            e.Accepted = (item.TimKiem ?? "").Contains(keyword);
        }

        private void CongThucSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => ApplyCongThucSearch();

        // =========================
        // DETAIL: FILTER + SORT + STT
        // =========================
        private void ApplySuDungSearch()
        {
            if (_suDungViewSource.View == null) return;
            _suDungViewSource.View.Refresh();

            _suDungViewSource.View.SortDescriptions.Clear();
            _suDungViewSource.View.SortDescriptions.Add(
                new SortDescription(nameof(SuDungNguyenLieuDto.LastModified), ListSortDirection.Descending));

            var view = _suDungViewSource.View.Cast<SuDungNguyenLieuDto>().ToList();
            int stt = 1;
            foreach (var x in view)
                x.Stt = stt++;
        }

        private void SuDungViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not SuDungNguyenLieuDto item)
            {
                e.Accepted = false;
                return;
            }

            // chỉ hiện theo công thức đang chọn
            if (_selectedCongThuc == null || _selectedCongThuc.Id == Guid.Empty)
            {
                e.Accepted = false;
                return;
            }

            if (item.CongThucId != _selectedCongThuc.Id)
            {
                e.Accepted = false;
                return;
            }

            var keyword = StringHelper.MyNormalizeText(SuDungSearchTextBox.Text.Trim());
            if (string.IsNullOrEmpty(keyword))
            {
                e.Accepted = true;
                return;
            }

            e.Accepted = item.TimKiem?.Contains(keyword) ?? false;
        }

        private void SuDungSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => ApplySuDungSearch();

        // =========================
        // SELECTION CHANGED
        // =========================
        private void CongThucDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCongThuc = CongThucDataGrid.SelectedItem as CongThucDto;

            RefreshDetailTitle();
            ToggleDetailButtons();
            ApplySuDungSearch();
        }

        private void RefreshDetailTitle()
        {
            if (_selectedCongThuc == null)
            {
                return;
            }

            var sp = _selectedCongThuc.TenSanPham ?? "";
            var bt = _selectedCongThuc.TenBienThe ?? "";
        }

        private void ToggleDetailButtons()
        {
            bool enable = _selectedCongThuc != null && _selectedCongThuc.Id != Guid.Empty;

            SuDungAddButton.IsEnabled = enable;
            SuDungEditButton.IsEnabled = enable;
            SuDungDeleteButton.IsEnabled = enable;
            SuDungReloadButton.IsEnabled = true; // vẫn cho reload list
        }

        // =========================
        // MASTER BUTTONS
        // =========================
        private async void CongThucReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.CongThucs.ReloadAsync();
            ApplyCongThucSearch();
        }

        private async void CongThucAddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new CongThucEdit();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
            {
                await AppProviders.CongThucs.ReloadAsync();
                ApplyCongThucSearch();
            }
        }

        private async void CongThucEditButton_Click(object sender, RoutedEventArgs e)
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
            {
                await AppProviders.CongThucs.ReloadAsync();
                ApplyCongThucSearch();
            }
        }

        private async void CongThucDeleteButton_Click(object sender, RoutedEventArgs e)
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
                {
                    AppProviders.CongThucs.Remove(selected.Id);
                    ApplyCongThucSearch();
                }
                else
                {
                    throw new Exception(result?.Message ?? "Không thể xoá.");
                }
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

        private async void CongThucDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CongThucDataGrid.SelectedItem is not CongThucDto selected) return;

            var window = new CongThucEdit(selected);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
            {
                await AppProviders.CongThucs.ReloadAsync();
                ApplyCongThucSearch();
            }
        }

        // =========================
        // DETAIL BUTTONS
        // =========================
        private async void SuDungReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.SuDungNguyenLieus.ReloadAsync();
            ApplySuDungSearch();
        }

        private async void SuDungAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCongThuc == null || _selectedCongThuc.Id == Guid.Empty)
            {
                MessageBox.Show("Vui lòng chọn công thức ở bên trái.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new SuDungNguyenLieuEdit(_selectedCongThuc, null);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
            {
                await AppProviders.SuDungNguyenLieus.ReloadAsync();
                ApplySuDungSearch();
            }
        }

        private async void SuDungEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCongThuc == null || _selectedCongThuc.Id == Guid.Empty)
            {
                MessageBox.Show("Vui lòng chọn công thức ở bên trái.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SuDungNguyenLieuDataGrid.SelectedItem is not SuDungNguyenLieuDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new SuDungNguyenLieuEdit(_selectedCongThuc, selected);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
            {
                await AppProviders.SuDungNguyenLieus.ReloadAsync();
                ApplySuDungSearch();
            }
        }

        private async void SuDungDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCongThuc == null || _selectedCongThuc.Id == Guid.Empty)
            {
                MessageBox.Show("Vui lòng chọn công thức ở bên trái.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SuDungNguyenLieuDataGrid.SelectedItem is not SuDungNguyenLieuDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá nguyên liệu '{selected.TenNguyenLieu}' khỏi công thức này?",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response = await ApiClient.DeleteAsync($"/api/SuDungNguyenLieu/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<SuDungNguyenLieuDto>>();

                if (result?.IsSuccess == true)
                {
                    AppProviders.SuDungNguyenLieus.Remove(selected.Id);
                    ApplySuDungSearch();
                }
                else
                {
                    throw new Exception(result?.Message ?? "Không thể xoá.");
                }
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

        private async void SuDungNguyenLieuDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_selectedCongThuc == null || _selectedCongThuc.Id == Guid.Empty) return;
            if (SuDungNguyenLieuDataGrid.SelectedItem is not SuDungNguyenLieuDto selected) return;

            var window = new SuDungNguyenLieuEdit(_selectedCongThuc, selected);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
            {
                await AppProviders.SuDungNguyenLieus.ReloadAsync();
                ApplySuDungSearch();
            }
        }

        // =========================
        // HOTKEYS (thông minh theo focus)
        // =========================
        private void CongThucMasterDetail_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool focusCongThuc = CongThucDataGrid.IsKeyboardFocusWithin;
            bool focusSuDung = SuDungNguyenLieuDataGrid.IsKeyboardFocusWithin;

            // Ctrl+N
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            {
                if (focusSuDung) SuDungAddButton_Click(null!, null!);
                else CongThucAddButton_Click(null!, null!);
                e.Handled = true;
                return;
            }

            // Ctrl+E
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            {
                if (focusSuDung) SuDungEditButton_Click(null!, null!);
                else CongThucEditButton_Click(null!, null!);
                e.Handled = true;
                return;
            }

            // Delete
            if (e.Key == Key.Delete)
            {
                if (focusSuDung) SuDungDeleteButton_Click(null!, null!);
                else CongThucDeleteButton_Click(null!, null!);
                e.Handled = true;
                return;
            }

            // F5
            if (e.Key == Key.F5)
            {
                if (focusSuDung) SuDungReloadButton_Click(null!, null!);
                else CongThucReloadButton_Click(null!, null!);
                e.Handled = true;
                return;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
