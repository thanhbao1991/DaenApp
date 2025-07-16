using TraSuaApp.Infrastructure.Data;

namespace TraSuaApp.WpfClient.Helpers
{
    public static class SeedHelper
    {
        public static async Task SeedNhomSanPhamAsync()
        {
            using var context = new AppDbContextFactory().CreateDbContext();

            var danhSachCu = new (int idOld, string ten)[]
            {
                (1, "Cà Phê"), (2, "Bạc Xỉu"), (3, "Ca Cao"), (4, "Sữa Chua"),
                (5, "Latte"), (6, "Soda"), (7, "Đá Xay"), (8, "Sinh Tố"),
                (9, "Nước Ép"), (10, "Trà"), (11, "U40 U50"), (12, "Sữa Tươi"),
                (13, "Trà Sữa"), (14, "Topping"), (15, "Voucher"), (16, "Nước Lon"),
                (17, "Thuốc"), (18, "Ăn Vặt"), (19, "Khác")
            };

            foreach (var (idOld, ten) in danhSachCu)
            {
                //if (!await context.NhomSanPhams.AnyAsync(x => x.IdOld == idOld))
                //{
                //    context.NhomSanPhams.Add(new NhomSanPham
                //    {
                //        Id = Guid.NewGuid(),
                //        Ten = ten
                //    });
                //}
            }

            await context.SaveChangesAsync();
        }
    }
}