﻿using System.ComponentModel;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class NhomSanPhamList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        string _friendlyName = TuDien._tableFriendlyNames["NhomSanPham"];

        public NhomSanPhamList()
        {
            InitializeComponent();

            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _viewSource.Source = AppProviders.NhomSanPhams.Items;
            _viewSource.Filter += ViewSource_Filter;
            NhomSanPhamDataGrid.ItemsSource = _viewSource.View;

            this.PreviewKeyDown += NhomSanPhamList_PreviewKeyDown;

            AppProviders.NhomSanPhams.OnChanged += () => ApplySearch();

            _ = AppProviders.NhomSanPhams.ReloadAsync();
        }

        private void ApplySearch()
        {
            _viewSource.View.Refresh();

            _viewSource.View.SortDescriptions.Clear();
            _viewSource.View.SortDescriptions.Add(new SortDescription(nameof(NhomSanPhamDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<NhomSanPhamDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].STT = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not NhomSanPhamDto item)
            {
                e.Accepted = false;
                return;
            }

            var keyword = TextSearchHelper.NormalizeText(SearchTextBox.Text.Trim());
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }
        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.NhomSanPhams.ReloadAsync();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new NhomSanPhamEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };
            if (window.ShowDialog() == true)
                await AppProviders.NhomSanPhams.ReloadAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (NhomSanPhamDataGrid.SelectedItem is not NhomSanPhamDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new NhomSanPhamEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };

            if (window.ShowDialog() == true)
                await AppProviders.NhomSanPhams.ReloadAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (NhomSanPhamDataGrid.SelectedItem is not NhomSanPhamDto selected)
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
                var response = await ApiClient.DeleteAsync($"/api/NhomSanPham/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<NhomSanPhamDto>>();
                if (result?.IsSuccess == true)
                    AppProviders.NhomSanPhams.Remove(selected.Id);
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

        private async void NhomSanPhamDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (NhomSanPhamDataGrid.SelectedItem is not NhomSanPhamDto selected) return;
            var window = new NhomSanPhamEdit(selected);
            if (window.ShowDialog() == true)
                await AppProviders.NhomSanPhams.ReloadAsync();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void NhomSanPhamList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N) { AddButton_Click(null!, null!); e.Handled = true; }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E) { EditButton_Click(null!, null!); e.Handled = true; }
            else if (e.Key == Key.Delete) { DeleteButton_Click(null!, null!); e.Handled = true; }
            else if (e.Key == Key.F5) { ReloadButton_Click(null!, null!); e.Handled = true; }
        }


    }
}