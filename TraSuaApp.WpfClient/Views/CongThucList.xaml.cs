using System.ComponentModel;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Config;
using TraSuaApp.WpfClient.DataProviders;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class CongThucMasterDetailList : Window
    {
        private readonly CollectionViewSource _congThucViewSource = new();
        private readonly CollectionViewSource _suDungViewSource = new();

        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["CongThuc"];

        private Guid? _editingCongThucId;
        private CongThucDto? _selectedCongThuc;
        private List<SanPhamDto> _sanPhamList = new();

        public CongThucMasterDetailList()
        {
            InitializeComponent();

            Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            PreviewKeyDown += CongThucMasterDetail_PreviewKeyDown;
            Closed += CongThucMasterDetailList_Closed;

            _congThucViewSource.Filter += CongThucViewSource_Filter;
            _suDungViewSource.Filter += SuDungViewSource_Filter;

            BindCongThucSource();
            BindSuDungSource();

            AppProviders.CongThucs.OnChanged += AppProviders_CongThucs_OnChanged;
            AppProviders.SuDungNguyenLieus.OnChanged += AppProviders_SuDungNguyenLieus_OnChanged;
            AppProviders.SanPhams.OnChanged += AppProviders_SanPhams_OnChanged;

            SanPhamSearchBox.SanPhamBienTheSelected += SanPhamSearchBox_SanPhamBienTheSelected;

            Loaded += async (_, __) =>
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    if (AppProviders.SanPhams.Items.Count == 0)
                        await AppProviders.SanPhams.ReloadAsync();

                    if (AppProviders.CongThucs.Items.Count == 0)
                        await AppProviders.CongThucs.ReloadAsync();

                    if (AppProviders.SuDungNguyenLieus.Items.Count == 0)
                        await AppProviders.SuDungNguyenLieus.ReloadAsync();

                    RefreshSanPhamList();
                    EnrichCongThucs();

                    BindCongThucSource();
                    BindSuDungSource();

                    ApplyCongThucSearch();
                    ApplySuDungSearch();

                    if (CongThucDataGrid.Items.Count > 0)
                        CongThucDataGrid.SelectedIndex = 0;
                    else
                        SetCongThucAddMode();
                }
                catch (Exception ex)
                {
                    _errorHandler.Handle(ex, "Load");
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            };
        }

        private void CongThucMasterDetailList_Closed(object? sender, EventArgs e)
        {
            AppProviders.CongThucs.OnChanged -= AppProviders_CongThucs_OnChanged;
            AppProviders.SuDungNguyenLieus.OnChanged -= AppProviders_SuDungNguyenLieus_OnChanged;
            AppProviders.SanPhams.OnChanged -= AppProviders_SanPhams_OnChanged;
        }

        private void AppProviders_CongThucs_OnChanged()
        {
            EnrichCongThucs();
            BindCongThucSource();
            ApplyCongThucSearch();
        }

        private void AppProviders_SuDungNguyenLieus_OnChanged()
        {
            BindSuDungSource();
            ApplySuDungSearch();
        }

        private void AppProviders_SanPhams_OnChanged()
        {
            RefreshSanPhamList();
            EnrichCongThucs();
            BindCongThucSource();
            ApplyCongThucSearch();
            RefreshFormFromSelectedCongThuc();
        }

        private void RefreshSanPhamList()
        {
            _sanPhamList = AppProviders.SanPhams.Items
                .Where(x => !x.NgungBan)
                .ToList();

            SanPhamSearchBox.SanPhamList = _sanPhamList;
        }

        private void EnrichCongThucs()
        {
            if (AppProviders.CongThucs?.Items == null || AppProviders.SanPhams?.Items == null)
                return;

            foreach (var ct in AppProviders.CongThucs.Items)
            {
                var sp = AppProviders.SanPhams.Items.FirstOrDefault(s =>
                    s.BienThe.Any(bt => bt.Id == ct.SanPhamBienTheId));

                var bt = sp?.BienThe.FirstOrDefault(b => b.Id == ct.SanPhamBienTheId);

                ct.TenSanPham = sp?.Ten;
                ct.TenBienThe = bt?.TenBienThe;
            }
        }

        private void BindCongThucSource()
        {
            _congThucViewSource.Source = AppProviders.CongThucs.Items;
            CongThucDataGrid.ItemsSource = _congThucViewSource.View;
        }

        private void BindSuDungSource()
        {
            _suDungViewSource.Source = AppProviders.SuDungNguyenLieus.Items;
            SuDungNguyenLieuDataGrid.ItemsSource = _suDungViewSource.View;
        }

        private void ApplyCongThucSearch()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplyCongThucSearch);
                return;
            }

            if (_congThucViewSource.View == null)
                return;

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
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }

        private void ApplySuDungSearch()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplySuDungSearch);
                return;
            }

            if (_suDungViewSource.View == null)
                return;

            _suDungViewSource.View.Refresh();

            _suDungViewSource.View.SortDescriptions.Clear();
            _suDungViewSource.View.SortDescriptions.Add(
                new SortDescription(nameof(SuDungNguyenLieuDto.TenNguyenLieu), ListSortDirection.Descending));

            var view = _suDungViewSource.View.Cast<SuDungNguyenLieuDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void SuDungViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not SuDungNguyenLieuDto item)
            {
                e.Accepted = false;
                return;
            }

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
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }

        private void CongThucSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyCongThucSearch();
        }

        private void SuDungSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySuDungSearch();
        }

        private void CongThucDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedCongThuc = CongThucDataGrid.SelectedItem as CongThucDto;

            RefreshFormFromSelectedCongThuc();
            RefreshDetailTitle();
            ToggleDetailButtons();
            ApplySuDungSearch();
        }

        private void RefreshFormFromSelectedCongThuc()
        {
            if (_selectedCongThuc == null)
                return;

            SetCongThucEditMode(_selectedCongThuc);
        }

        private void RefreshDetailTitle()
        {
            if (_selectedCongThuc == null)
            {
                SelectedCongThucTextBlock.Text = "Chưa chọn công thức.";
                return;
            }

            var sp = _selectedCongThuc.TenSanPham ?? "";
            var bt = _selectedCongThuc.TenBienThe ?? "";
            SelectedCongThucTextBlock.Text = $"{sp} - {bt}".Trim(' ', '-');
        }

        private void ToggleDetailButtons()
        {
            bool enable = _selectedCongThuc != null && _selectedCongThuc.Id != Guid.Empty;

            SuDungAddButton.IsEnabled = enable;
            SuDungEditButton.IsEnabled = enable;
            SuDungDeleteButton.IsEnabled = enable;
            SuDungReloadButton.IsEnabled = true;
        }

        private void SetCongThucAddMode()
        {
            _editingCongThucId = null;

            CongThucFormTitleTextBlock.Text = "Thêm công thức";
            CongThucFormInfoTextBlock.Text = "Chọn sản phẩm / biến thể rồi nhập thông tin công thức.";
            SaveCongThucButton.Content = "Thêm";
            ErrorTextBlock.Text = string.Empty;

            SanPhamSearchBox.Clear();
            TenTextBox.Text = string.Empty;
            LoaiTextBox.Text = "Default";
            IsDefaultCheckBox.IsChecked = true;

            TenTextBox.Focus();
        }

        private void SetCongThucEditMode(CongThucDto selected)
        {
            _editingCongThucId = selected.Id;

            ErrorTextBlock.Text = string.Empty;

            var sp = _sanPhamList.FirstOrDefault(s => s.BienThe.Any(bt => bt.Id == selected.SanPhamBienTheId));
            var bt = sp?.BienThe.FirstOrDefault(b => b.Id == selected.SanPhamBienTheId);

            if (sp != null && bt != null)
                SanPhamSearchBox.SetSelectedSanPham(sp, bt);
            else
                SanPhamSearchBox.Clear();

            TenTextBox.Text = selected.Ten ?? string.Empty;
            LoaiTextBox.Text = selected.Loai ?? string.Empty;
            IsDefaultCheckBox.IsChecked = selected.IsDefault;

            SetCongThucControlsEnabled(true);
        }

        private void SetCongThucControlsEnabled(bool enabled)
        {
            SanPhamSearchBox.IsEnabled = enabled;
            TenTextBox.IsEnabled = enabled;
            LoaiTextBox.IsEnabled = enabled;
            IsDefaultCheckBox.IsEnabled = enabled;
            SaveCongThucButton.IsEnabled = true;
        }

        private void SanPhamSearchBox_SanPhamBienTheSelected(object? sp, SanPhamBienTheDto? bt)
        {
            if (sp == null || bt == null)
                return;

            if (string.IsNullOrWhiteSpace(TenTextBox.Text))
                TenTextBox.Text = $" {bt.TenBienThe}".Trim();

            if (string.IsNullOrWhiteSpace(LoaiTextBox.Text))
                LoaiTextBox.Text = "Default";
        }

        private int FindCongThucIndex(Guid id)
        {
            for (int i = 0; i < AppProviders.CongThucs.Items.Count; i++)
            {
                if (AppProviders.CongThucs.Items[i].Id == id)
                    return i;
            }

            return -1;
        }

        private async void CongThucReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await ReloadCongThucAsync();
        }

        private async Task ReloadCongThucAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                await AppProviders.CongThucs.ReloadAsync();

                EnrichCongThucs();
                BindCongThucSource();
                ApplyCongThucSearch();

                if (CongThucDataGrid.Items.Count > 0)
                    CongThucDataGrid.SelectedIndex = 0;
                else
                    SetCongThucAddMode();
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "CongThucReload");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void CongThucAddButton_Click(object sender, RoutedEventArgs e)
        {
            SetCongThucAddMode();
        }

        private void CongThucEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (CongThucDataGrid.SelectedItem is not CongThucDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetCongThucEditMode(selected);
        }

        private async void CongThucDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (CongThucDataGrid.SelectedItem is not CongThucDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} cho '{selected.TenSanPham} - {selected.TenBienThe}'?",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response = await ApiClient.DeleteAsync($"/api/CongThuc/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<CongThucDto>>();

                if (result?.IsSuccess == true)
                {
                    var id = result.Data?.Id != Guid.Empty ? result!.Data!.Id : selected.Id;
                    var index = FindCongThucIndex(id);

                    if (index >= 0)
                        AppProviders.CongThucs.Items.RemoveAt(index);
                    else
                        await AppProviders.CongThucs.ReloadAsync();

                    NotiHelper.ShowSuccess(result.Message);
                    EnrichCongThucs();
                    BindCongThucSource();
                    ApplyCongThucSearch();

                    if (AppProviders.CongThucs.Items.Count > 0)
                        CongThucDataGrid.SelectedIndex = 0;
                    else
                    {
                        _selectedCongThuc = null;
                        RefreshDetailTitle();
                        ToggleDetailButtons();
                        ApplySuDungSearch();
                        SetCongThucAddMode();
                    }
                }
                else
                {
                    NotiHelper.ShowError(result?.Message ?? "Không thể xoá.");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "CongThucDelete");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void CongThucDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (CongThucDataGrid.SelectedItem is not CongThucDto selected)
                return;

            SetCongThucEditMode(selected);
        }

        private async void SaveCongThucButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = string.Empty;

            if (SanPhamSearchBox.SelectedSanPham == null || SanPhamSearchBox.SelectedBienThe == null)
            {
                ErrorTextBlock.Text = "Vui lòng chọn sản phẩm / biến thể.";
                SanPhamSearchBox.SearchTextBox.Focus();
                return;
            }

            var ten = TenTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(ten))
            {
                ErrorTextBlock.Text = "Tên công thức không được để trống.";
                TenTextBox.Focus();
                return;
            }

            var dto = new CongThucDto
            {
                Id = _editingCongThucId ?? Guid.Empty,
                SanPhamBienTheId = SanPhamSearchBox.SelectedBienThe.Id,
                Ten = ten,
                Loai = LoaiTextBox.Text.Trim(),
                IsDefault = IsDefaultCheckBox.IsChecked == true,
                TenSanPham = SanPhamSearchBox.SelectedSanPham.Ten,
                TenBienThe = SanPhamSearchBox.SelectedBienThe.TenBienThe
            };

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                Result<CongThucDto> result;

                if (_editingCongThucId == null)
                    result = await Apis.CongThuc.CreateAsync(dto);
                else
                    result = await Apis.CongThuc.UpdateAsync(_editingCongThucId.Value, dto);

                if (!result.IsSuccess)
                {
                    ErrorTextBlock.Text = result.Message;
                    return;
                }

                if (result.Data != null)
                {
                    var sp = _sanPhamList.FirstOrDefault(s => s.BienThe.Any(bt => bt.Id == result.Data.SanPhamBienTheId));
                    var bt = sp?.BienThe.FirstOrDefault(b => b.Id == result.Data.SanPhamBienTheId);

                    result.Data.TenSanPham = sp?.Ten;
                    result.Data.TenBienThe = bt?.TenBienThe;

                    var index = FindCongThucIndex(result.Data.Id);

                    if (index >= 0)
                        AppProviders.CongThucs.Items[index] = result.Data;
                    else
                        AppProviders.CongThucs.Items.Add(result.Data);

                    NotiHelper.ShowSuccess(result.Message);

                    EnrichCongThucs();
                    BindCongThucSource();
                    ApplyCongThucSearch();

                    CongThucDataGrid.SelectedItem = result.Data;
                    CongThucDataGrid.ScrollIntoView(result.Data);
                    SetCongThucEditMode(result.Data);
                }
                else
                {
                    await AppProviders.CongThucs.ReloadAsync();
                    EnrichCongThucs();
                    BindCongThucSource();
                    ApplyCongThucSearch();
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "SaveCongThuc");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void CancelCongThucButton_Click(object sender, RoutedEventArgs e)
        {
            if (_editingCongThucId == null)
            {
                SetCongThucAddMode();
                return;
            }

            if (CongThucDataGrid.SelectedItem is CongThucDto selected)
                SetCongThucEditMode(selected);
            else
                SetCongThucAddMode();
        }

        private async void SuDungReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.SuDungNguyenLieus.ReloadAsync();
            ApplySuDungSearch();
        }

        private async void SuDungAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCongThuc == null || _selectedCongThuc.Id == Guid.Empty)
            {
                MessageBox.Show("Vui lòng chọn công thức ở bên trái.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new SuDungNguyenLieuEdit(_selectedCongThuc, null)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

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
                MessageBox.Show("Vui lòng chọn công thức ở bên trái.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SuDungNguyenLieuDataGrid.SelectedItem is not SuDungNguyenLieuDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new SuDungNguyenLieuEdit(_selectedCongThuc, selected)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

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
                MessageBox.Show("Vui lòng chọn công thức ở bên trái.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (SuDungNguyenLieuDataGrid.SelectedItem is not SuDungNguyenLieuDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá nguyên liệu '{selected.TenNguyenLieu}' khỏi công thức này?",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

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
                    NotiHelper.ShowError(result?.Message ?? "Không thể xoá.");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "SuDungDelete");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void SuDungNguyenLieuDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_selectedCongThuc == null || _selectedCongThuc.Id == Guid.Empty)
                return;

            if (SuDungNguyenLieuDataGrid.SelectedItem is not SuDungNguyenLieuDto selected)
                return;

            var window = new SuDungNguyenLieuEdit(_selectedCongThuc, selected)
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (window.ShowDialog() == true)
            {
                await AppProviders.SuDungNguyenLieus.ReloadAsync();
                ApplySuDungSearch();
            }
        }

        private void CongThucMasterDetail_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool focusDetail = SuDungNguyenLieuDataGrid.IsKeyboardFocusWithin;

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            {
                if (focusDetail)
                    SuDungAddButton_Click(null!, null!);
                else
                    CongThucAddButton_Click(null!, null!);

                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            {
                if (focusDetail)
                    SuDungEditButton_Click(null!, null!);
                else
                    CongThucEditButton_Click(null!, null!);

                e.Handled = true;
                return;
            }

            if (e.Key == Key.Delete)
            {
                if (focusDetail)
                    SuDungDeleteButton_Click(null!, null!);
                else
                    CongThucDeleteButton_Click(null!, null!);

                e.Handled = true;
                return;
            }

            if (e.Key == Key.F5)
            {
                if (focusDetail)
                    SuDungReloadButton_Click(null!, null!);
                else
                    CongThucReloadButton_Click(null!, null!);

                e.Handled = true;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}