using System.ComponentModel;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.AdminViews
{
    public partial class SuDungNguyenLieuList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();

        // ✅ Lưu luôn CongThucDto, để dùng Id + show title nếu cần
        private readonly CongThucDto _congThuc;
        private readonly string _friendlyName = "Sử dụng nguyên liệu";

        // =========================================
        //  CTOR KHÔNG THAM SỐ (mở từ menu → báo lỗi)
        // =========================================
        public SuDungNguyenLieuList()
            : this(new CongThucDto { Id = Guid.Empty })
        {
            MessageBox.Show(
                "Vui lòng thêm/sửa nguyên liệu từ màn Công thức.",
                "Thông báo",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            // form này được gọi nhầm → đóng luôn
            Close();
        }

        // =========================================
        //  CTOR CHÍNH – MỞ TỪ MÀN CÔNG THỨC
        // =========================================
        public SuDungNguyenLieuList(CongThucDto congThuc)
        {
            InitializeComponent();

            _congThuc = congThuc;

            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;
            this.PreviewKeyDown += SuDungNguyenLieuList_PreviewKeyDown;

            // chờ AppProviders nếu dùng pattern global
            while (AppProviders.SuDungNguyenLieus?.Items == null)
            {
                Task.Delay(100); // giống các form khác của anh
            }

            // 1. Gán source
            _viewSource.Source = AppProviders.SuDungNguyenLieus.Items;
            _viewSource.Filter += ViewSource_Filter;
            SuDungNguyenLieuDataGrid.ItemsSource = _viewSource.View;

            // 2. OnChanged
            AppProviders.SuDungNguyenLieus.OnChanged += () => ApplySearch();

            // 3. Reload lần đầu
            Loaded += async (_, __) =>
            {
                await AppProviders.SuDungNguyenLieus.ReloadAsync();
                ApplySearch();
            };
        }

        // =========================================
        //  LỌC + SẮP XẾP + STT
        // =========================================
        private void ApplySearch()
        {
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

            // chỉ lấy nguyên liệu thuộc đúng công thức đang chọn
            if (item.CongThucId != _congThuc.Id)
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

            e.Accepted = item.TimKiem?.Contains(keyword) ?? false;
        }

        // =========================================
        //  TOOLBAR
        // =========================================
        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.SuDungNguyenLieus.ReloadAsync();
            ApplySearch();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // mở form edit, truyền đúng CongThucDto
            var window = new SuDungNguyenLieuEdit(_congThuc, null);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
                await AppProviders.SuDungNguyenLieus.ReloadAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (SuDungNguyenLieuDataGrid.SelectedItem is not SuDungNguyenLieuDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new SuDungNguyenLieuEdit(_congThuc, selected);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
                await AppProviders.SuDungNguyenLieus.ReloadAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
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
                    AppProviders.SuDungNguyenLieus.Remove(selected.Id);
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

        // =========================================
        //  DOUBLE CLICK
        // =========================================
        private async void SuDungNguyenLieuDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SuDungNguyenLieuDataGrid.SelectedItem is not SuDungNguyenLieuDto selected)
                return;

            var window = new SuDungNguyenLieuEdit(_congThuc, selected);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true)
                await AppProviders.SuDungNguyenLieus.ReloadAsync();
        }

        // =========================================
        //  SEARCH BOX
        // =========================================
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
            => ApplySearch();

        // =========================================
        //  KHÁC
        // =========================================
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

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