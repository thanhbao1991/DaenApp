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
    public partial class TaiKhoanList : Window
    {
        private readonly CollectionViewSource _viewSource = new();
        private readonly WpfErrorHandler _errorHandler = new();
        string _friendlyName = TuDien._tableFriendlyNames["TaiKhoan"];

        public TaiKhoanList()
        {
            InitializeComponent();

            this.Title = _friendlyName;
            TieuDeTextBlock.Text = _friendlyName;

            _viewSource.Source = AppProviders.TaiKhoans.Items;
            _viewSource.Filter += ViewSource_Filter;
            TaiKhoanDataGrid.ItemsSource = _viewSource.View;

            this.PreviewKeyDown += TaiKhoanList_PreviewKeyDown;

            AppProviders.TaiKhoans.OnChanged += () => ApplySearch();

            _ = AppProviders.TaiKhoans.ReloadAsync();
        }

        private void ApplySearch()
        {
            _viewSource.View.Refresh();

            _viewSource.View.SortDescriptions.Clear();
            _viewSource.View.SortDescriptions.Add(new SortDescription(nameof(TaiKhoanDto.LastModified), ListSortDirection.Descending));

            var view = _viewSource.View.Cast<TaiKhoanDto>().ToList();
            for (int i = 0; i < view.Count; i++)
                view[i].STT = i + 1;
        }

        private void ViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is not TaiKhoanDto item)
            {
                e.Accepted = false;
                return;
            }

            var keyword = TextSearchHelper.NormalizeText(SearchTextBox.Text.Trim());
            e.Accepted = string.IsNullOrEmpty(keyword) || (item.TimKiem?.Contains(keyword) ?? false);
        }
        private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            await AppProviders.TaiKhoans.ReloadAsync();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new TaiKhoanEdit()
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };

            if (window.ShowDialog() == true)
                await AppProviders.TaiKhoans.ReloadAsync();
        }

        private async void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaiKhoanDataGrid.SelectedItem is not TaiKhoanDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần sửa.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new TaiKhoanEdit(selected)
            {
                Width = this.ActualWidth,
                Height = this.ActualHeight
            };
            if (window.ShowDialog() == true)
                await AppProviders.TaiKhoans.ReloadAsync();
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (TaiKhoanDataGrid.SelectedItem is not TaiKhoanDto selected)
            {
                MessageBox.Show("Vui lòng chọn dòng cần xoá.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var confirm = MessageBox.Show(
                $"Bạn có chắc chắn muốn xoá {_friendlyName} '{selected.TenDangNhap}'?",
                "Xác nhận xoá", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await ApiClient.DeleteAsync($"/api/TaiKhoan/{selected.Id}");
                var result = await response.Content.ReadFromJsonAsync<Result<TaiKhoanDto>>();
                if (result?.IsSuccess == true)
                    AppProviders.TaiKhoans.Remove(selected.Id);
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

        private async void TaiKhoanDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TaiKhoanDataGrid.SelectedItem is not TaiKhoanDto selected) return;
            var window = new TaiKhoanEdit(selected);
            if (window.ShowDialog() == true)
                await AppProviders.TaiKhoans.ReloadAsync();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplySearch();

        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void TaiKhoanList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N) { AddButton_Click(null!, null!); e.Handled = true; }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E) { EditButton_Click(null!, null!); e.Handled = true; }
            else if (e.Key == Key.Delete) { DeleteButton_Click(null!, null!); e.Handled = true; }
            else if (e.Key == Key.F5) { ReloadButton_Click(null!, null!); e.Handled = true; }
        }


    }
}