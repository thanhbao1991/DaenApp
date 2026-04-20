using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.WpfClient.DataProviders;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class SuDungNguyenLieuList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = "Sử dụng nguyên liệu";

        private readonly CongThucDto _congThuc;
        private Guid? _editingId;
        private List<NguyenLieuBanHangDto> _nguyenLieuBanHangList = new();

        public SuDungNguyenLieuList()
            : this(new CongThucDto { Id = Guid.Empty })
        {
        }

        public SuDungNguyenLieuList(CongThucDto congThuc)
        {
            InitializeComponent();

            _congThuc = congThuc;

            Title = _friendlyName;
            TieuDeTextBlock.Text = string.IsNullOrWhiteSpace(congThuc.TenSanPham)
                ? _friendlyName
                : $"{_friendlyName} - {congThuc.TenSanPham} - {congThuc.TenBienThe}";

            PreviewKeyDown += SuDungNguyenLieuList_PreviewKeyDown;
            Closed += SuDungNguyenLieuList_Closed;

            _viewSource.Filter += ViewSource_Filter;
            BindSource();

            AppProviders.SuDungNguyenLieus.OnChanged += AppProviders_SuDungNguyenLieus_OnChanged;
            AppProviders.NguyenLieuBanHangs.OnChanged += AppProviders_NguyenLieuBanHangs_OnChanged;

            Loaded += async (_, __) =>
            {
                if (_congThuc.Id == Guid.Empty)
                {
                    MessageBox.Show(
                        "Vui lòng thêm/sửa nguyên liệu từ màn Công thức.",
                        "Thông báo",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    Close();
                    return;
                }

                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    if (AppProviders.NguyenLieuBanHangs.Items.Count == 0)
                        await AppProviders.NguyenLieuBanHangs.ReloadAsync();

                    RefreshNguyenLieuBanHangList();

                    await AppProviders.SuDungNguyenLieus.ReloadAsync();
                    BindSource();
                    ApplySearch();
                    SetAddMode();
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

        private void SuDungNguyenLieuList_Closed(object? sender, EventArgs e)
        {
            AppProviders.SuDungNguyenLieus.OnChanged -= AppProviders_SuDungNguyenLieus_OnChanged;
            AppProviders.NguyenLieuBanHangs.OnChanged -= AppProviders_NguyenLieuBanHangs_OnChanged;
        }

        private void AppProviders_SuDungNguyenLieus_OnChanged()
        {
            BindSource();
            ApplySearch();
        }

        private void AppProviders_NguyenLieuBanHangs_OnChanged()
        {
            RefreshNguyenLieuBanHangList();
        }

        private void RefreshNguyenLieuBanHangList(IEnumerable<Guid>? selectedIds = null)
        {
            var ids = selectedIds?.ToHashSet() ?? new HashSet<Guid>();

            _nguyenLieuBanHangList = AppProviders.NguyenLieuBanHangs.Items.ToList();
            NguyenLieuBanHangSearchBox.NguyenLieuBanHangList = _nguyenLieuBanHangList;

            if (_editingId == null)
            {
                NguyenLieuBanHangSearchBox.SearchTextBox.Text = string.Empty;
                return;
            }

            if (ids.Count == 0)
                return;
        }

        private void BindSource()
        {
            _viewSource.Source = AppProviders.SuDungNguyenLieus.Items;
            SuDungNguyenLieuDataGrid.ItemsSource = _viewSource.View;
        }

        private void ApplySearch()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(ApplySearch);
                return;
            }

            if (_viewSource.View == null)
                return;

            _viewSource.View.Refresh();

            _viewSource.View.SortDescriptions.Clear();
            _viewSource.View.SortDescriptions.Add(
                new SortDescription(nameof(SuDungNguyenLieuDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<SuDungNguyenLieuDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].Stt = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not SuDungNguyenLieuDto item)
            {
                e.Accepted = false;
                return;
            }

            if (_congThuc.Id != Guid.Empty && item.CongThucId != _congThuc.Id)
            {
                e.Accepted = false;
                return;
            }

            var keyword = StringHelper.MyNormalizeText(SearchTextBox.Text.Trim());
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }

        private void SetAddMode()
        {
            _editingId = null;
            FormTitleTextBlock.Text = "Thêm sử dụng nguyên liệu";
            SaveButton.Content = "Thêm";

            NguyenLieuBanHangSearchBox.SearchTextBox.Text = string.Empty;
            TenTextBox_ClearIfExists();
            SoLuongNumeric.Value = 1;
            GhiChuTextBox.Text = string.Empty;
            ErrorTextBlock.Text = string.Empty;

            SuDungNguyenLieuDataGrid.UnselectAll();
            NguyenLieuBanHangSearchBox.SearchTextBox.Focus();
        }

        private void SetEditMode(SuDungNguyenLieuDto selected)
        {
            _editingId = selected.Id;
            FormTitleTextBlock.Text = "Sửa sử dụng nguyên liệu";


            RefreshNguyenLieuBanHangList();

            if (selected.NguyenLieuId != Guid.Empty)
                NguyenLieuBanHangSearchBox.SetSelectedNguyenLieuBanHangByIdWithoutPopup(selected.NguyenLieuId);

            SoLuongNumeric.Value = selected.SoLuong;
            GhiChuTextBox.Text = selected.GhiChu ?? string.Empty;
            ErrorTextBlock.Text = string.Empty;


            {
                NguyenLieuBanHangSearchBox.IsEnabled = true;
                SoLuongNumeric.IsEnabled = true;
                GhiChuTextBox.IsEnabled = true;
            }

            NguyenLieuBanHangSearchBox.SearchTextBox.Focus();
        }

        private void TenTextBox_ClearIfExists()
        {
            if (FindName("TenTextBox") is TextBox tb)
                tb.Text = string.Empty;
        }

        private int FindIndex(Guid id)
        {
            for (int i = 0; i < AppProviders.SuDungNguyenLieus.Items.Count; i++)
            {
                if (AppProviders.SuDungNguyenLieus.Items[i].Id == id)
                    return i;
            }

            return -1;
        }

        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await ReloadAsync();
        }

        private async Task ReloadAsync()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                await AppProviders.SuDungNguyenLieus.ReloadAsync();
                BindSource();
                ApplySearch();
                SetAddMode();
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Reload");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            SetAddMode();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SuDungNguyenLieuDataGrid.SelectedItem is not SuDungNguyenLieuDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            SetEditMode(selected);
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = string.Empty;

            var selectedNl = NguyenLieuBanHangSearchBox.SelectedNguyenLieuBanHang;
            if (selectedNl == null)
            {
                ErrorTextBlock.Text = "Vui lòng chọn nguyên liệu.";
                NguyenLieuBanHangSearchBox.SearchTextBox.Focus();
                return;
            }

            if (SoLuongNumeric.Value <= 0)
            {
                ErrorTextBlock.Text = "Số lượng phải lớn hơn 0.";
                SoLuongNumeric.Focus();
                return;
            }

            var dto = new SuDungNguyenLieuDto
            {
                CongThucId = _congThuc.Id,
                NguyenLieuId = selectedNl.Id,
                SoLuong = SoLuongNumeric.Value,
                GhiChu = (GhiChuTextBox.Text ?? string.Empty).Trim(),

            };

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                HttpResponseMessage response;

                if (_editingId == null)
                {
                    response = await ApiClient.PostAsync("/api/SuDungNguyenLieu", dto);
                }
                else
                {
                    response = await ApiClient.PutAsync($"/api/SuDungNguyenLieu/{_editingId.Value}", dto);
                }

                if (!response.IsSuccessStatusCode)
                {
                    ErrorTextBlock.Text = $"{(_editingId == null ? "Thêm" : "Cập nhật")} thất bại ({(int)response.StatusCode}).";
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<SuDungNguyenLieuDto>>();

                if (result?.IsSuccess == true)
                {
                    if (result.Data != null)
                    {
                        var index = FindIndex(result.Data.Id);

                        if (index >= 0)
                            AppProviders.SuDungNguyenLieus.Items[index] = result.Data;
                        else
                            AppProviders.SuDungNguyenLieus.Items.Add(result.Data);
                    }
                    else
                    {
                        await AppProviders.SuDungNguyenLieus.ReloadAsync();
                    }

                    NotiHelper.ShowSuccess(result.Message);
                    BindSource();
                    ApplySearch();
                    SetAddMode();
                }
                else
                {
                    await AppProviders.SuDungNguyenLieus.ReloadAsync();
                    BindSource();
                    ApplySearch();
                    SetAddMode();
                    NotiHelper.ShowSuccess(_editingId == null ? "Thêm thành công." : "Cập nhật thành công.");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Save");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
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

                if (!response.IsSuccessStatusCode)
                {
                    NotiHelper.ShowError($"Xoá thất bại ({(int)response.StatusCode}).");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<SuDungNguyenLieuDto>>();

                var id = result?.Data?.Id != Guid.Empty
                    ? result!.Data!.Id
                    : selected.Id;

                var index = FindIndex(id);

                if (index >= 0)
                    AppProviders.SuDungNguyenLieus.Items.RemoveAt(index);
                else
                    await AppProviders.SuDungNguyenLieus.ReloadAsync();

                NotiHelper.ShowSuccess(result?.Message ?? "Xoá thành công.");
                BindSource();
                ApplySearch();
                SetAddMode();
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

        private void CancelEditButton_Click(object sender, RoutedEventArgs e)
        {
            SetAddMode();
        }

        private void SuDungNguyenLieuDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SuDungNguyenLieuDataGrid.SelectedItem is not SuDungNguyenLieuDto selected)
                return;

            SetEditMode(selected);
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SuDungNguyenLieuList_PreviewKeyDown(object sender, KeyEventArgs e)
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