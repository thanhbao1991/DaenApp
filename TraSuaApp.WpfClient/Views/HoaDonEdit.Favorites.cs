using System.Windows;
using System.Windows.Controls;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.WpfClient.HoaDonViews
{
    public partial class HoaDonEdit
    {
        private void RenderFavoriteChipsFromText(string? raw)
        {
            try
            {
                GoiYWrap.Children.Clear();

                if (string.IsNullOrWhiteSpace(raw))
                    return;

                var parts = raw.Split(';', ',', '/', '|')
                    .Select(x => x.Trim())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .ToList();

                foreach (var name in parts)
                {
                    var sp = FindSanPhamByNameLoose(name);
                    if (sp == null) continue;

                    var btn = new Button
                    {
                        Content = name,
                        Tag = sp.Id,
                        Style = FindResource("AddButtonStyle") as Style
                    };

                    btn.Click += FavoriteChip_Click;

                    GoiYWrap.Children.Add(btn);
                }
            }
            catch { }
        }

        private void FavoriteChip_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is not Guid spId) return;

            var sp = _sanPhamList.FirstOrDefault(x => x.Id == spId);
            if (sp == null) return;

            var bt = sp.BienThe.OrderBy(x => x.GiaBan).FirstOrDefault();
            if (bt == null) return;

            var ct = new ChiTietHoaDonDto
            {
                Id = Guid.NewGuid(),
                TenSanPham = sp.Ten,
                TenBienThe = bt.TenBienThe,
                SanPhamIdBienThe = bt.Id,
                SoLuong = 1
            };

            decimal donGia = bt.GiaBan;

            if (Model.KhachHangId != null
                && !string.Equals(Model.PhanLoai, "App", StringComparison.OrdinalIgnoreCase))
            {
                var customGia = AppProviders.KhachHangGiaBans.Items
                    .FirstOrDefault(x =>
                        x.KhachHangId == Model.KhachHangId.Value &&
                        x.SanPhamBienTheId == bt.Id &&
                        !x.IsDeleted);

                if (customGia != null)
                {
                    donGia = customGia.GiaBan;
                }
            }

            ct.DonGia = donGia;

            AttachLineWatcher(ct);

            Model.ChiTietHoaDons.Add(ct);

            UpdateTotals();

            SaveButton.Focus();
        }
    }
}

