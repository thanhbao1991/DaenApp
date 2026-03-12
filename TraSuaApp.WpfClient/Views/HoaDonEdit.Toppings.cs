using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Dtos;


namespace TraSuaApp.WpfClient.HoaDonViews
{
    public partial class HoaDonEdit
    {
        private void LoadToppingPanel(Guid? nhomSanPhamId)
        {
            var dsTopping = _toppingList
                .Where(t => t.NhomSanPhams.Contains(nhomSanPhamId ?? Guid.Empty))
                .OrderBy(x => x.Ten)
                .ToList();

            ToppingListBox.ItemsSource = dsTopping;
        }

        private void ToppingPlus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ToppingDto topping)
            {
                topping.SoLuong++;
                ToppingListBox.Items.Refresh();
                CapNhatToppingChoSanPham();
            }
        }

        private void ToppingMinus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ToppingDto topping)
            {
                if (topping.SoLuong > 0)
                    topping.SoLuong--;

                ToppingListBox.Items.Refresh();
                CapNhatToppingChoSanPham();
            }
        }
    }
}