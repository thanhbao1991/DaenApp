using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
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

namespace TraSuaApp.WpfClient.SettingsViews
{
    public partial class SanPhamList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        private readonly string _friendlyName = TuDien._tableFriendlyNames["SanPham"];
        private void SetAddMode()
        {
            _editingId = null;
            _editingVariantId = null;
            _variants = new ObservableCollection<SanPhamBienTheDto>();

            SaveButton.Content = "Lưu";

            TenTextBox.Text = string.Empty;
            VietTatTextBox.Text = string.Empty;
            TichDiemCheck.IsChecked = true;
            NgungBanCheck.IsChecked = false;
            ComboNhom.SelectedIndex = -1;

            BienTheTextBox.Text = string.Empty;
            GiaBanTextBox.Value = 0;
            DinhLuongVariantTextBox.Text = string.Empty; // ✅ thêm

            SaveVariantButton.Content = "Thêm biến thể";

            BienTheListBox.ItemsSource = _variants;
            BienTheListBox.UnselectAll();

            SaveButton.IsEnabled = true;
            SetControlsEnabled(true);

            ErrorTextBlock.Text = string.Empty;
            TenTextBox.Focus();
        }
        private void SetEditMode(SanPhamDto selected)
        {
            _editingId = selected.Id;
            _editingVariantId = null;

            TenTextBox.Text = selected.Ten;
            VietTatTextBox.Text = selected.VietTat;
            TichDiemCheck.IsChecked = selected.TichDiem;
            NgungBanCheck.IsChecked = selected.NgungBan;
            ComboNhom.SelectedValue = selected.NhomSanPhamId;

            _variants = selected.BienThe != null
                ? new ObservableCollection<SanPhamBienTheDto>(
                    selected.BienThe.Select(x => new SanPhamBienTheDto
                    {
                        Id = x.Id,
                        TenBienThe = x.TenBienThe,
                        GiaBan = x.GiaBan,
                        DinhLuong = x.DinhLuong, // ✅ thêm
                        MacDinh = x.MacDinh
                    }))
                : new ObservableCollection<SanPhamBienTheDto>();

            BienTheListBox.ItemsSource = _variants;
            BienTheListBox.UnselectAll();

            BienTheTextBox.Text = string.Empty;
            GiaBanTextBox.Value = 0;
            DinhLuongVariantTextBox.Text = string.Empty; // ✅ thêm

            SaveVariantButton.Content = "Thêm biến thể";
            ErrorTextBlock.Text = string.Empty;

            TenTextBox.Focus();
            TenTextBox.SelectAll();
        }
        private void ClearVariantEditor()
        {
            _editingVariantId = null;
            BienTheTextBox.Text = string.Empty;
            GiaBanTextBox.Value = 0;
            DinhLuongVariantTextBox.Text = string.Empty; // ✅ thêm

            SaveVariantButton.Content = "Thêm biến thể";
            BienTheListBox.UnselectAll();
        }
        private void PopulateVariantEditor(SanPhamBienTheDto selected)
        {
            _editingVariantId = selected.Id;
            BienTheTextBox.Text = selected.TenBienThe;
            GiaBanTextBox.Value = selected.GiaBan;
            DinhLuongVariantTextBox.Text = selected.DinhLuong; // ✅ thêm

            SaveVariantButton.Content = "Cập nhật biến thể";
        }
        private bool CommitVariantInput()
        {
            var name = BienTheTextBox.Text.Trim();
            var priceText = GiaBanTextBox.Text.Trim();
            var dinhLuong = DinhLuongVariantTextBox.Text.Trim(); // ✅ thêm

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(priceText))
                return true;

            if (string.IsNullOrWhiteSpace(name))
            {
                ErrorTextBlock.Text = "Tên biến thể không được để trống.";
                BienTheTextBox.Focus();
                return false;
            }

            if (!decimal.TryParse(GiaBanTextBox.Text.Replace(",", "").Trim(), out var price) || price < 0)
            {
                ErrorTextBlock.Text = "Giá bán biến thể không hợp lệ.";
                GiaBanTextBox.Focus();
                return false;
            }

            if (_editingVariantId.HasValue)
            {
                var existing = _variants.FirstOrDefault(x => x.Id == _editingVariantId.Value);
                if (existing != null)
                {
                    existing.TenBienThe = name;
                    existing.GiaBan = price;
                    existing.DinhLuong = dinhLuong; // ✅ thêm
                }
            }
            else
            {
                _variants.Add(new SanPhamBienTheDto
                {
                    Id = Guid.Empty,
                    TenBienThe = name,
                    GiaBan = price,
                    DinhLuong = dinhLuong, // ✅ thêm
                    MacDinh = _variants.Count == 0
                });
            }

            BienTheListBox.Items.Refresh();
            ClearVariantEditor();
            return true;
        }
        private void SetControlsEnabled(bool enabled)
        {
            TenTextBox.IsEnabled = enabled;
            VietTatTextBox.IsEnabled = enabled;
            TichDiemCheck.IsEnabled = enabled;
            NgungBanCheck.IsEnabled = enabled;
            ComboNhom.IsEnabled = enabled;

            // ❌ XÓA dòng này
            // DinhLuongTextBox.IsEnabled = enabled;

            BienTheTextBox.IsEnabled = enabled;
            GiaBanTextBox.IsEnabled = enabled;
            DinhLuongVariantTextBox.IsEnabled = enabled; // ✅ thêm

            SaveVariantButton.IsEnabled = enabled;
            CancelVariantButton.IsEnabled = enabled;
            BienTheListBox.IsEnabled = enabled;
        }
        private Guid? _editingId;
        private Guid? _editingVariantId;
        private ObservableCollection<SanPhamBienTheDto> _variants = new();
        private void SanPhamDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SanPhamDataGrid.SelectedItem is not SanPhamDto selected)
                return;

            SetEditMode(selected);
            ShowForm();

            SanPhamDataGrid.ScrollIntoView(selected); // 🟟 thêm
        }
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SanPhamDataGrid.SelectedItem is not SanPhamDto selected)
            {
                MessageBox.Show($"Vui lòng chọn {_friendlyName} cần sửa.");
                return;
            }

            SetEditMode(selected);
            ShowForm();

            SanPhamDataGrid.ScrollIntoView(selected); // 🟟 thêm
        }
        public SanPhamList()
        {
            InitializeComponent();

            Title = _friendlyName;
            PreviewKeyDown += SanPhamList_PreviewKeyDown;
            Closed += SanPhamList_Closed;

            ComboNhom.ItemsSource = AppProviders.NhomSanPhams.Items;
            ComboNhom.DisplayMemberPath = nameof(NhomSanPhamDto.Ten);
            ComboNhom.SelectedValuePath = nameof(NhomSanPhamDto.Id);

            _viewSource.Filter += ViewSource_Filter;
            BindSource();

            AppProviders.SanPhams.OnChanged += AppProviders_SanPhams_OnChanged;
            AppProviders.NhomSanPhams.OnChanged += AppProviders_NhomSanPhams_OnChanged;

            Loaded += async (_, __) =>
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    if (AppProviders.NhomSanPhams.Items.Count == 0)
                        await AppProviders.NhomSanPhams.ReloadAsync();

                    await AppProviders.SanPhams.ReloadAsync();

                    RefreshNhomCombo();
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

        private void SanPhamList_Closed(object? sender, EventArgs e)
        {
            AppProviders.SanPhams.OnChanged -= AppProviders_SanPhams_OnChanged;
            AppProviders.NhomSanPhams.OnChanged -= AppProviders_NhomSanPhams_OnChanged;
        }

        private void AppProviders_SanPhams_OnChanged()
        {
            BindSource();
            ApplySearch();
        }

        private void AppProviders_NhomSanPhams_OnChanged()
        {
            RefreshNhomCombo();
        }

        private void RefreshNhomCombo()
        {
            ComboNhom.ItemsSource = AppProviders.NhomSanPhams.Items;
        }

        private void BindSource()
        {
            _viewSource.Source = AppProviders.SanPhams.Items;
            SanPhamDataGrid.ItemsSource = _viewSource.View;
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
                new SortDescription(nameof(SanPhamDto.LastModified), ListSortDirection.Descending));

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


        private int FindIndex(Guid id)
        {
            for (int i = 0; i < AppProviders.SanPhams.Items.Count; i++)
            {
                if (AppProviders.SanPhams.Items[i].Id == id)
                    return i;
            }

            return -1;
        }


        private void DieuChinhGiaSizeL()
        {
            if (_variants == null || _variants.Count < 2)
                return;

            var sizeChuan = _variants.FirstOrDefault(x =>
                x.TenBienThe.ToLower().Contains("chuẩn") ||
                x.TenBienThe.ToLower() == "m");

            var sizeL = _variants.FirstOrDefault(x =>
                x.TenBienThe.ToLower() == "l" ||
                x.TenBienThe.ToLower().Contains("size l"));

            if (sizeChuan != null && sizeL != null && sizeChuan.GiaBan == sizeL.GiaBan)
            {
                sizeL.GiaBan = sizeChuan.GiaBan + 5000;
            }
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

                if (AppProviders.NhomSanPhams.Items.Count == 0)
                    await AppProviders.NhomSanPhams.ReloadAsync();

                await AppProviders.SanPhams.ReloadAsync();

                RefreshNhomCombo();
                BindSource();
                ApplySearch();
                SetAddMode();
                HideForm();
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
            ShowForm(); // 🟟 thêm dòng này
        }


        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (SanPhamDataGrid.SelectedItem is not SanPhamDto selected)
            {
                MessageBox.Show(
                    $"Vui lòng chọn {_friendlyName} cần xoá.",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} '{selected.Ten}'?",
                "Xác nhận xoá",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                var response = await ApiClient.DeleteAsync($"/api/SanPham/{selected.Id}");

                if (!response.IsSuccessStatusCode)
                {
                    NotiHelper.ShowError($"Xoá thất bại ({(int)response.StatusCode}).");
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<SanPhamDto>>();

                var id = result?.Data?.Id != Guid.Empty ? result!.Data!.Id : selected.Id;
                var index = FindIndex(id);

                if (index >= 0)
                    AppProviders.SanPhams.Items.RemoveAt(index);
                else
                    await AppProviders.SanPhams.ReloadAsync();

                NotiHelper.ShowSuccess(result?.Message ?? "Xoá thành công.");
                BindSource();
                ApplySearch();
                SetAddMode();
                HideForm();
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

        private void SaveVariantButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CommitVariantInput())
                return;
        }

        private void CancelVariantButton_Click(object sender, RoutedEventArgs e)
        {
            ClearVariantEditor();
        }

        private void VariantListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BienTheListBox.SelectedItem is SanPhamBienTheDto v)
            {
                PopulateVariantEditor(v);
            }
        }

        private void XoaVariant_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not SanPhamBienTheDto v)
                return;

            if (_editingVariantId == v.Id)
                ClearVariantEditor();

            _variants.Remove(v);
            BienTheListBox.Items.Refresh();
        }

        private void MacDinhCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.DataContext is SanPhamBienTheDto sel)
            {
                foreach (var v in _variants)
                    v.MacDinh = (v == sel);

                BienTheListBox.Items.Refresh();
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = string.Empty;

            if (!CommitVariantInput())
                return;

            var ten = TenTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(ten))
            {
                ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                TenTextBox.Focus();
                return;
            }

            if (ComboNhom.SelectedIndex == -1)
            {
                ErrorTextBlock.Text = $"Nhóm {_friendlyName} không được để trống.";
                ComboNhom.Focus();
                return;
            }

            var model = _editingId == null ? new SanPhamDto() : new SanPhamDto
            {
                Id = _editingId.Value
            };

            model.Ten = ten
                .Replace("TCDĐ", "TCĐĐ")
                .Replace("TCĐD", "TCĐĐ")
                .Replace("TCDD", "TCĐĐ")
                .Trim();
            model.VietTat = VietTatTextBox.Text.Trim();
            model.NgungBan = NgungBanCheck.IsChecked == true;
            model.TichDiem = TichDiemCheck.IsChecked == true;
            model.NhomSanPhamId = (Guid)ComboNhom.SelectedValue;
            model.BienThe = _variants.ToList();

            if (model.BienThe.Any() && !model.BienThe.Any(x => x.MacDinh))
                model.BienThe[0].MacDinh = true;

            DieuChinhGiaSizeL();

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                HttpResponseMessage response;

                if (_editingId == null)
                    response = await ApiClient.PostAsync("/api/SanPham", model);
                else
                    response = await ApiClient.PutAsync($"/api/SanPham/{_editingId.Value}", model);

                if (!response.IsSuccessStatusCode)
                {
                    ErrorTextBlock.Text = $"{(_editingId == null ? "Thêm" : "Cập nhật")} thất bại ({(int)response.StatusCode}).";
                    return;
                }

                var result = await response.Content.ReadFromJsonAsync<Result<SanPhamDto>>();

                if (result?.IsSuccess == true)
                {
                    if (result.Data != null)
                    {
                        // 🟟 map TenNhom từ provider
                        var nhom = AppProviders.NhomSanPhams.Items
                            .FirstOrDefault(x => x.Id == result.Data.NhomSanPhamId);

                        if (nhom != null)
                        {
                            result.Data.TenNhomSanPham = nhom.Ten; // 🟟 QUAN TRỌNG
                        }

                        var index = FindIndex(result.Data.Id);

                        if (index >= 0)
                            AppProviders.SanPhams.Items[index] = result.Data;
                        else
                            AppProviders.SanPhams.Items.Add(result.Data);
                    }
                    else
                    {
                        await AppProviders.SanPhams.ReloadAsync();
                    }

                    NotiHelper.ShowSuccess(result.Message);
                    BindSource();
                    ApplySearch();
                    SetAddMode();
                    HideForm(); // 🟟 thêm dòng này
                }
                else
                {
                    await AppProviders.SanPhams.ReloadAsync();
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

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

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
        private void ShowForm()
        {
            FormPanel.Visibility = Visibility.Visible;
        }

        private void HideForm()
        {
            FormPanel.Visibility = Visibility.Collapsed;
        }
    }
}