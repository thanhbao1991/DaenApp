using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.AdminViews;

public partial class NguyenLieuTransactionList : Window
{
    private readonly CollectionViewSource _viewSource = new();

    public NguyenLieuTransactionList()
    {
        InitializeComponent();

        _viewSource.Source = AppProviders.NguyenLieuTransactions.Items;
        _viewSource.Filter += Filter;

        NguyenLieuTransactionDataGrid.ItemsSource = _viewSource.View;

        Loaded += async (_, _) =>
        {
            await AppProviders.NguyenLieuTransactions.ReloadAsync();
            Refresh();
        };

        AppProviders.NguyenLieuTransactions.OnChanged += Refresh;
    }

    private void Refresh()
    {
        _viewSource.View.Refresh();

        var list = _viewSource.View.Cast<NguyenLieuTransactionDto>().ToList();
        for (int i = 0; i < list.Count; i++)
            list[i].Stt = i + 1;
    }

    private void Filter(object sender, FilterEventArgs e)
    {
        if (e.Item is not NguyenLieuTransactionDto item)
        {
            e.Accepted = false;
            return;
        }

        var keyword = StringHelper.MyNormalizeText(SearchTextBox.Text);
        e.Accepted = string.IsNullOrEmpty(keyword) ||
                     (item.TimKiem?.Contains(keyword) ?? false);
    }

    private async void ReloadButton_Click(object sender, RoutedEventArgs e)
        => await AppProviders.NguyenLieuTransactions.ReloadAsync();

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var w = new NguyenLieuTransactionEdit();
        if (w.ShowDialog() == true)
            await AppProviders.NguyenLieuTransactions.ReloadAsync();
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (NguyenLieuTransactionDataGrid.SelectedItem is not NguyenLieuTransactionDto item)
            return;

        var w = new NguyenLieuTransactionEdit(item);
        if (w.ShowDialog() == true)
            await AppProviders.NguyenLieuTransactions.ReloadAsync();
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (NguyenLieuTransactionDataGrid.SelectedItem is not NguyenLieuTransactionDto item)
            return;

        if (MessageBox.Show("Xoá giao dịch này?",
                "Xác nhận", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        await ApiClient.DeleteAsync($"/api/NguyenLieuTransaction/{item.Id}");
        await AppProviders.NguyenLieuTransactions.ReloadAsync();
    }

    private void NguyenLieuTransactionDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        => EditButton_Click(null!, null!);

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        => Refresh();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();
}
