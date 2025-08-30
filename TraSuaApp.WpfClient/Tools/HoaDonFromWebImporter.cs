//using System.Data;
//using Dapper;

//namespace TraSuaApp.Import
//{
//    public class HoaDonFromWebImporter
//    {
//        private readonly IDbConnection newDb;

//        public HoaDonFromWebImporter(IDbConnection newDb)
//        {
//            this.newDb = newDb;
//        }

//        public async Task ImportAsync(ShippingOrderDto order)
//        {
//            // --- Kiểm tra trùng theo MaHoaDon ---
//            var existingId = await newDb.QueryFirstOrDefaultAsync<Guid?>(@"
//                SELECT Id FROM HoaDons WHERE MaHoaDon = @MaHoaDon
//            ", new { MaHoaDon = order.Code }); // order.Code = mã đơn từ web

//            if (existingId != null)
//            {
//                Console.WriteLine($"⚠ Hóa đơn {order.Code} đã tồn tại, bỏ qua.");
//                return;
//            }

//            Guid hoaDonId = Guid.NewGuid();

//            // --- Insert Hóa Đơn ---
//            await newDb.ExecuteAsync(@"
//                INSERT INTO HoaDons(Id, MaHoaDon, NgayTao, TongTien, GhiChu, CreatedAt, LastModified)
//                VALUES (@Id, @MaHoaDon, @NgayTao, @TongTien, @GhiChu, @Created, @Modified)
//            ", new
//            {
//                Id = hoaDonId,
//                MaHoaDon = order.Code,   // giữ đúng mã web
//                NgayTao = order.OrderDate,
//                TongTien = order.TotalAmount,
//                GhiChu = order.Note,
//                Created = DateTime.Now,
//                Modified = DateTime.Now
//            });

//            // --- Insert Chi Tiết ---
//            foreach (var item in order.Items)
//            {
//                Guid ctId = Guid.NewGuid();

//                // Phân loại option: size & topping
//                var sizeOpt = item.Options.FirstOrDefault(o => o.Name.Contains("Size", StringComparison.OrdinalIgnoreCase));
//                var toppingOpts = item.Options.Where(o => o != sizeOpt).ToList();

//                Guid? sanPhamBienTheId = null;

//                if (sizeOpt != null)
//                {
//                    sanPhamBienTheId = await newDb.QueryFirstOrDefaultAsync<Guid?>(@"
//                        SELECT Id 
//                        FROM SanPhamBienThes 
//                        WHERE TenBienThe LIKE @BienThe
//                          AND TenSanPham LIKE @SanPham
//                    ", new
//                    {
//                        BienThe = "%" + sizeOpt.Name + "%",
//                        SanPham = "%" + item.ProductName + "%"
//                    });
//                }
//                else
//                {
//                    sanPhamBienTheId = await newDb.QueryFirstOrDefaultAsync<Guid?>(@"
//                        SELECT TOP 1 Id 
//                        FROM SanPhamBienThes 
//                        WHERE TenSanPham LIKE @SanPham
//                          AND IsDefault = 1
//                    ", new { SanPham = "%" + item.ProductName + "%" });

//                    if (sanPhamBienTheId == null)
//                    {
//                        sanPhamBienTheId = await newDb.QueryFirstOrDefaultAsync<Guid?>(@"
//                            SELECT TOP 1 Id 
//                            FROM SanPhamBienThes 
//                            WHERE TenSanPham LIKE @SanPham
//                              AND TenBienThe LIKE '%Size L%'
//                        ", new { SanPham = "%" + item.ProductName + "%" });
//                    }

//                    if (sanPhamBienTheId == null)
//                    {
//                        Console.WriteLine($"⚠ Không tìm thấy biến thể cho sản phẩm: {item.ProductName}");
//                        continue;
//                    }
//                }

//                // Insert món chính
//                await newDb.ExecuteAsync(@"
//                    INSERT INTO ChiTietHoaDons(Id, HoaDonId, SanPhamBienTheId, SoLuong, DonGia, ThanhTien, CreatedAt, LastModified)
//                    VALUES (@Id, @HoaDonId, @BienTheId, @SL, @Gia, @TT, @Created, @Modified)
//                ", new
//                {
//                    Id = ctId,
//                    HoaDonId = hoaDonId,
//                    BienTheId = sanPhamBienTheId,
//                    SL = item.Quantity,
//                    Gia = item.UnitPrice,
//                    TT = item.TotalPrice,
//                    Created = DateTime.Now,
//                    Modified = DateTime.Now
//                });

//                // Insert topping
//                foreach (var tp in toppingOpts)
//                {
//                    Guid toppingId = Guid.NewGuid();

//                    await newDb.ExecuteAsync(@"
//                        INSERT INTO ChiTietHoaDonToppings(Id, ChiTietHoaDonId, TenTopping, SoLuong, CreatedAt, LastModified)
//                        VALUES (@Id, @CTId, @TenTP, @SL, @Created, @Modified)
//                    ", new
//                    {
//                        Id = toppingId,
//                        CTId = ctId,
//                        TenTP = tp.Name,
//                        SL = tp.Quantity,
//                        Created = DateTime.Now,
//                        Modified = DateTime.Now
//                    });
//                }
//            }

//            Console.WriteLine($"✔ Import thành công hóa đơn {order.Code}");
//        }

//        public async Task ImportManyAsync(List<ShippingOrderDto> orders)
//        {
//            foreach (var order in orders)
//            {
//                await ImportAsync(order);
//            }
//        }
//    }
//}