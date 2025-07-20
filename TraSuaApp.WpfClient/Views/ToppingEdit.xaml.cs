using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Apis;
using TraSuaApp.WpfClient.Models;
using TraSuaApp.WpfClient.Services;

namespace TraSuaApp.WpfClient.Views
{
    public partial class ToppingEdit : Window
    {
        public ToppingDto Model { get; set; } = new();
        private readonly IToppingApi _api;
        private readonly string _friendlyName = TuDien._tableFriendlyNames["Topping"];
        private List<NhomSanPhamCheckItem> _bindingList = new();

        public ToppingEdit(ToppingDto? dto = null)
        {
            InitializeComponent();
            this.KeyDown += Window_KeyDown;
            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _api = new ToppingApi();

            if (dto != null)
            {
                Model = dto;
                TenTextBox.Text = dto.Ten;
            }
            else
            {
                TenTextBox.Focus();
            }

            if (Model.IsDeleted)
            {
                TenTextBox.IsEnabled = false;
                SaveButton.Content = "Khôi phục";
            }

            LoadGroupsFromProvider();
        }

        private void LoadGroupsFromProvider()
        {
            // Lấy danh sách nhóm sản phẩm đã được nạp sẵn
            var groups = AppProviders.NhomSanPhams.Items;
            _bindingList = groups.Select(g => new NhomSanPhamCheckItem
            {
                Id = g.Id,
                Ten = g.Ten,
                IsChecked = Model.IdNhomSanPhams?.Contains(g.Id) == true
            }).ToList();

            NhomSanPhamListBox.ItemsSource = _bindingList;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = "";

            Model.Ten = TenTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(Model.Ten))
            {
                ErrorTextBlock.Text = $"Tên {_friendlyName} không được để trống.";
                TenTextBox.Focus();
                return;
            }

            // Cập nhật danh sách IdNhomSanPhams từ checkbox
            Model.IdNhomSanPhams = _bindingList
                .Where(x => x.IsChecked)
                .Select(x => x.Id)
                .ToList();

            Result<ToppingDto> result;
            if (Model.Id == Guid.Empty)
                result = await _api.CreateAsync(Model);
            else if (Model.IsDeleted)
                result = await _api.RestoreAsync(Model.Id);
            else
                result = await _api.UpdateAsync(Model.Id, Model);

            if (!result.IsSuccess)
            {
                ErrorTextBlock.Text = result.Message;
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseButton_Click(null!, null!);
                return;
            }

            if (e.Key == Key.Enter)
            {
                if (Keyboard.FocusedElement is Button) return;

                var request = new TraversalRequest(FocusNavigationDirection.Next);
                if (Keyboard.FocusedElement is UIElement element)
                {
                    element.MoveFocus(request);
                    e.Handled = true;
                }
            }
        }

        // Giữ event handler để tránh cảnh báo XAML
        private void NhomSanPhamListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}