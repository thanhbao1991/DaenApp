﻿using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class NhomSanPhamEdit : Window
    {
        public NhomSanPhamDto Data { get; private set; }
        private readonly bool _isEdit;
        private readonly WpfErrorHandler _errorHandler;

        public NhomSanPhamEdit(NhomSanPhamDto? dto = null)
        {
            InitializeComponent();
            _errorHandler = new WpfErrorHandler(ErrorTextBlock);
            _isEdit = dto != null;
            Data = dto ?? new NhomSanPhamDto();

            LoadForm();
            this.KeyDown += Window_KeyDown;
        }

        private void LoadForm()
        {
            TenTextBox.Text = Data.Ten;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _errorHandler.Clear();
            SaveButton.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            try
            {
                if (string.IsNullOrWhiteSpace(TenTextBox.Text))
                    throw new Exception("Tên nhóm là bắt buộc.");

                Data.Ten = TenTextBox.Text.Trim();

                HttpResponseMessage response = _isEdit
                    ? await ApiClient.PutAsync($"/api/nhomsanpham/{Data.Id}", Data)
                    : await ApiClient.PostAsync("/api/nhomsanpham", Data);

                var result = await response.Content.ReadFromJsonAsync<Result<NhomSanPhamDto>>();
                if (result?.IsSuccess == true)
                {
                    DialogResult = true;
                }
                else
                {
                    throw new Exception(result?.Message ?? "Lưu nhóm sản phẩm thất bại.");
                }
            }
            catch (Exception ex)
            {
                _errorHandler.Handle(ex, "Lưu nhóm sản phẩm");
            }
            finally
            {
                SaveButton.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SaveButton_Click(SaveButton, new RoutedEventArgs());
            else if (e.Key == Key.Escape)
                DialogResult = false;
        }
    }
}