using System.IO;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Data.SqlClient;

public class OldHoaDon
{
    public int IdHoaDon { get; set; }
    public int? IdKhachHang { get; set; }
    public int? IdNhomHoaDon { get; set; }
    public int? IdBan { get; set; }
    public DateTime? NgayHoaDon { get; set; }
    public decimal TongTien { get; set; }
    public decimal TienVoucher { get; set; }
    public decimal DaThu { get; set; }
    public decimal ConLai { get; set; }
    public decimal TienBank { get; set; }
    public DateTime? NgayBank { get; set; }
    public decimal TienNo { get; set; }
    public DateTime? NgayNo { get; set; }
    public string? DiaChiShip { get; set; }
    public string? DienThoaiShip { get; set; }
    public string? ThongTinHoaDon { get; set; }
    public string? TenBan { get; set; }
}

public class OldChiTietHoaDon
{
    public int IdChiTietHoaDon { get; set; }
    public int IdHoaDon { get; set; }
    public int IdSanPham { get; set; }
    public string TenSanPham { get; set; } = "";
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThanhTien { get; set; }
    public string? GhiChu { get; set; }
}

public class HoaDonImporter
{
    private readonly string _oldConn;
    private readonly string _newConn;

    public HoaDonImporter(string oldConnectionString, string newConnectionString)
    {
        _oldConn = oldConnectionString;
        _newConn = newConnectionString;
    }

    public async Task ImportTodayAsync()
    {
        using var oldDb = new SqlConnection(_oldConn);
        using var newDb = new SqlConnection(_newConn);

        var today = DateTime.Today.AddDays(-1);

        var hoadons = (await oldDb.QueryAsync<OldHoaDon>(@"
            SELECT 
                h.IdHoaDon,
                h.IdKhachHang,
                h.IdNhomHoaDon,
                h.IdBan,
                h.NgayHoaDon,
                h.TongTien,
                ISNULL((SELECT SUM(ct.ThanhTien) 
                        FROM ChiTietHoaDon ct 
                        WHERE ct.IdHoaDon = h.IdHoaDon AND ct.ThanhTien < 0), 0) AS TienVoucher,
                h.DaThu,
                h.ConLai,
                ISNULL(h.TienBank,0) AS TienBank,
                h.NgayBank,
                ISNULL(h.TienNo,0) AS TienNo,
                h.NgayNo,
                h.DiaChiShip,
                h.DienThoaiShip,
                h.ThongTinHoaDon,
                ISNULL(b.TenBan, '') AS TenBan
            FROM dbo.HoaDon h
            LEFT JOIN dbo.Ban b ON b.IdBan = h.IdBan
            WHERE h.IdNhomHoaDon < 10 
              AND CAST(h.NgayHoaDon AS DATE) = @Today
            ORDER BY h.IdHoaDon
        ", new { Today = today })).ToList();

        if (hoadons.Count == 0)
        {
            System.Windows.MessageBox.Show("✅ Không có hóa đơn hôm nay để import.");
            return;
        }

        var chitiets = (await oldDb.QueryAsync<OldChiTietHoaDon>(@"
            SELECT IdChiTietHoaDon, IdHoaDon, IdSanPham, TenSanPham, SoLuong, DonGia, ThanhTien, GhiChu
            FROM dbo.ChiTietHoaDon
        ")).ToList();

        var khMap = (await newDb.QueryAsync<(int OldId, Guid NewId)>(@"
            SELECT OldId, Id FROM KhachHangs
            WHERE OldId IS NOT NULL AND OldId <> 0
        ")).GroupBy(x => x.OldId).ToDictionary(g => g.Key, g => g.First().NewId);

        var spMapBase = (await newDb.QueryAsync<(int OldId, Guid NewId)>(@"
            SELECT OldId, Id FROM SanPhams
            WHERE OldId IS NOT NULL AND OldId <> 0
        ")).ToDictionary(x => x.OldId, x => x.NewId);

        var spBienTheMap = (await newDb.QueryAsync<(Guid SpId, Guid BienTheId, decimal Gia, string Ten, bool IsDefault)>(@"
            SELECT bt.SanPhamId AS SpId, bt.Id AS BienTheId, bt.GiaBan AS Gia, bt.TenBienThe AS Ten,
                   CAST(ISNULL(bt.MacDinh, 0) AS BIT) AS IsDefault
            FROM SanPhamBienThes bt
        ")).ToList();

        var voucherOldMap = (await newDb.QueryAsync<(int OldId, Guid Id)>(@"
            SELECT OldId, Id FROM Vouchers
            WHERE OldId IS NOT NULL AND OldId <> 0
        ")).ToDictionary(x => x.OldId, x => x.Id);

        var voucherNameMap = (await newDb.QueryAsync<(string Ten, Guid Id)>(@"
            SELECT LOWER(Ten) AS Ten, Id FROM Vouchers
        ")).ToDictionary(x => x.Ten, x => x.Id);

        var toppingMap = (await newDb.QueryAsync<(string Ten, Guid Id)>(@"
            SELECT LOWER(Ten) AS Ten, Id FROM Toppings
        ")).ToDictionary(x => x.Ten, x => x.Id);

        var phuongThucMap = (await newDb.QueryAsync<(string Ten, Guid Id)>(@"
            SELECT LOWER(Ten) AS Ten, Id FROM PhuongThucThanhToans
        ")).ToDictionary(x => x.Ten, x => x.Id);

        int added = 0, skipped = 0;
        var logMessages = new List<string>();

        foreach (var hd in hoadons)
        {
            try
            {
                decimal giamGiaHoaDon = hd.TienVoucher < 0 ? Math.Abs(hd.TienVoucher) : hd.TienVoucher;

                Guid? khId = null;
                if (hd.IdKhachHang.HasValue && khMap.TryGetValue(hd.IdKhachHang.Value, out var mappedKh))
                    khId = mappedKh;

                var cts = chitiets.Where(c => c.IdHoaDon == hd.IdHoaDon).OrderBy(c => c.IdChiTietHoaDon).ToList();
                if (cts.Count == 0)
                {
                    skipped++;
                    logMessages.Add($"⚠️ Bỏ qua hóa đơn {hd.IdHoaDon} - Không có chi tiết");
                    continue;
                }

                // 🟟 Kiểm tra lệch tiền
                decimal tongChiTiet = cts.Where(c => c.ThanhTien > 0).Sum(x => x.ThanhTien);
                decimal tongGiamGia = cts.Where(c => c.ThanhTien < 0).Sum(x => Math.Abs(x.ThanhTien));
                decimal thanhTienTinhLai = tongChiTiet - tongGiamGia;

                if (Math.Abs(thanhTienTinhLai - hd.TongTien) > 1 ||
                    Math.Abs(giamGiaHoaDon - tongGiamGia) > 1)
                {
                    skipped++;
                    logMessages.Add($"❌ Bỏ qua hóa đơn {hd.IdHoaDon} - Sai lệch số tiền");
                    continue;
                }

                // 🟟 Xác định voucherId nếu có
                Guid? hoaDonVoucherId = null;
                foreach (var ct in cts.Where(x => x.ThanhTien < 0))
                {
                    if (voucherOldMap.TryGetValue(ct.IdSanPham, out var vId))
                        hoaDonVoucherId = vId;
                    else
                    {
                        var match = voucherNameMap.FirstOrDefault(v => (ct.TenSanPham ?? "").ToLower().Contains(v.Key));
                        if (match.Value != Guid.Empty)
                            hoaDonVoucherId = match.Value;
                    }
                }

                string phanLoai = hd.IdNhomHoaDon switch
                {
                    1 => "Tại Chỗ",
                    2 => "MV",
                    3 => "Ship",
                    4 => "App",
                    _ => "Khác"
                };

                int stt = await newDb.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(*) FROM HoaDons
                    WHERE CAST(NgayGio AS DATE) = @Ngay AND PhanLoai = @PhanLoai
                ", new { Ngay = (hd.NgayHoaDon ?? DateTime.Now).Date, PhanLoai = phanLoai }) + 1;

                string tenBan = phanLoai switch
                {
                    "MV" => $"MV {stt}",
                    "Ship" => $"Ship {stt}",
                    "App" => $"App {stt}",
                    _ => string.IsNullOrWhiteSpace(hd.TenBan) ? "" : hd.TenBan
                };

                Guid hoaDonId = Guid.NewGuid();

                // ✅ Insert hóa đơn
                string trangThai = hd.DaThu >= hd.TongTien ? "Đã Thu"
                                : hd.DaThu == 0 ? "Ghi Nợ" : "Còn Nợ";

                await newDb.ExecuteAsync(@"
                    INSERT INTO HoaDons(Id, OldId, MaHoaDon, TrangThai, PhanLoai, TenBan, DiaChiText, SoDienThoaiText,
                                        KhachHangId, VoucherId, TongTien, GiamGia, ThanhTien, Ngay, NgayGio, CreatedAt, LastModified, IsDeleted)
                    VALUES (@Id, @OldId, @MaHoaDon, @TrangThai, @PhanLoai, @TenBan, @DiaChi, @Phone,
                            @KhId, @VoucherId, @TongTien, @GiamGia, @ThanhTien, @Ngay, @NgayGio, @CreatedAt, @LastModified, 0)
                ", new
                {
                    Id = hoaDonId,
                    OldId = hd.IdHoaDon,
                    MaHoaDon = $"HD{hd.IdHoaDon:D6}",
                    TrangThai = trangThai,
                    PhanLoai = phanLoai,
                    TenBan = tenBan,
                    DiaChi = hd.DiaChiShip ?? "",
                    Phone = hd.DienThoaiShip ?? "",
                    KhId = khId,
                    VoucherId = hoaDonVoucherId,
                    TongTien = tongChiTiet,
                    GiamGia = tongGiamGia,
                    ThanhTien = hd.TongTien,
                    Ngay = (hd.NgayHoaDon ?? DateTime.Now).Date,
                    NgayGio = hd.NgayHoaDon ?? DateTime.Now,
                    CreatedAt = hd.NgayHoaDon ?? DateTime.Now,
                    LastModified = hd.NgayHoaDon ?? DateTime.Now
                });

                // ✅ Thanh toán (tiền mặt, chuyển khoản)
                decimal tienMat = hd.DaThu - hd.TienBank;
                if (tienMat > 0 && phuongThucMap.TryGetValue("tiền mặt", out var tienMatId))
                {
                    await newDb.ExecuteAsync(@"
                        INSERT INTO ChiTietHoaDonThanhToans(Id, HoaDonId, PhuongThucThanhToanId, SoTien, Ngay, NgayGio)
                        VALUES (@Id, @HoaDonId, @PTTT, @SoTien, @Ngay, @NgayGio)
                    ", new
                    {
                        Id = Guid.NewGuid(),
                        HoaDonId = hoaDonId,
                        PTTT = tienMatId,
                        KhId = khId,
                        SoTien = tienMat,
                        Ngay = (hd.NgayHoaDon ?? DateTime.Now).Date,
                        NgayGio = hd.NgayHoaDon ?? DateTime.Now
                    });
                }

                if (hd.TienBank > 0 && phuongThucMap.TryGetValue("chuyển khoản", out var ckId))
                {
                    await newDb.ExecuteAsync(@"
                        INSERT INTO ChiTietHoaDonThanhToans(Id, HoaDonId, PhuongThucThanhToanId, SoTien, Ngay, NgayGio)
                        VALUES (@Id, @HoaDonId, @PTTT, @SoTien, @Ngay, @NgayGio)
                    ", new
                    {
                        Id = Guid.NewGuid(),
                        HoaDonId = hoaDonId,
                        PTTT = ckId,
                        KhId = khId,
                        SoTien = hd.TienBank,
                        Ngay = (hd.NgayBank ?? hd.NgayHoaDon ?? DateTime.Now).Date,
                        NgayGio = hd.NgayBank ?? hd.NgayHoaDon ?? DateTime.Now
                    });
                }

                // ✅ Nợ
                if (hd.TienNo > 0)
                {
                    await newDb.ExecuteAsync(@"
                        INSERT INTO ChiTietHoaDonNos(Id, HoaDonId, KhachHangId, SoTienNo, SoTienDaTra, Ngay, NgayGio)
                        VALUES (@Id, @HoaDonId, @KhId, @SoTienNo, 0, @Ngay, @NgayGio)
                    ", new
                    {
                        Id = Guid.NewGuid(),
                        HoaDonId = hoaDonId,
                        KhId = khId,
                        SoTienNo = hd.TienNo,
                        Ngay = (hd.NgayNo ?? hd.NgayHoaDon ?? DateTime.Now).Date,
                        NgayGio = hd.NgayNo ?? hd.NgayHoaDon ?? DateTime.Now
                    });
                }

                // ✅ Điểm tích lũy
                if (khId.HasValue)
                {
                    int diemTichLuy = (int)Math.Floor(hd.TongTien * 0.01m);
                    DateTime ngay = hd.NgayHoaDon ?? DateTime.Now;

                    await newDb.ExecuteAsync(@"
                        INSERT INTO DiemKhachHangLogs(Id, KhachHangId, ThoiGian, DiemThayDoi, GhiChu, CreatedAt, LastModified)
                        VALUES (@Id, @KhachHangId, @ThoiGian, @DiemThayDoi, @GhiChu, @CreatedAt, @LastModified)
                    ", new
                    {
                        Id = Guid.NewGuid(),
                        KhachHangId = khId.Value,
                        ThoiGian = ngay,
                        DiemThayDoi = diemTichLuy,
                        GhiChu = $"Tích điểm từ hoá đơn {hoaDonId}",
                        CreatedAt = ngay,
                        LastModified = ngay
                    });

                    var existingDiem = await newDb.ExecuteScalarAsync<int?>(@"
                        SELECT TongDiem FROM DiemKhachHangs WHERE KhachHangId = @KhId
                    ", new { KhId = khId.Value });

                    if (existingDiem == null)
                    {
                        await newDb.ExecuteAsync(@"
                            INSERT INTO DiemKhachHangs(Id, KhachHangId, TongDiem, CreatedAt, LastModified)
                            VALUES (@Id, @KhachHangId, @TongDiem, @CreatedAt, @LastModified)
                        ", new
                        {
                            Id = Guid.NewGuid(),
                            KhachHangId = khId.Value,
                            TongDiem = diemTichLuy,
                            CreatedAt = ngay,
                            LastModified = ngay
                        });
                    }
                    else
                    {
                        await newDb.ExecuteAsync(@"
                            UPDATE DiemKhachHangs
                            SET TongDiem = TongDiem + @Diem, LastModified = @LastModified
                            WHERE KhachHangId = @KhId
                        ", new { Diem = diemTichLuy, KhId = khId.Value, LastModified = ngay });
                    }
                }

                // ✅ Chi tiết hóa đơn + topping + voucher
                foreach (var ct in cts)
                {
                    if (ct.ThanhTien < 0)
                    {
                        Guid voucherId = hoaDonVoucherId ?? Guid.Empty;
                        if (voucherId != Guid.Empty)
                        {
                            string tenVoucher = (await newDb.QueryFirstOrDefaultAsync<string>(
                                "SELECT Ten FROM Vouchers WHERE Id = @Id", new { Id = voucherId }
                            )) ?? (ct.TenSanPham ?? "");

                            await newDb.ExecuteAsync(@"
                                INSERT INTO ChiTietHoaDonVouchers(Id, HoaDonId, VoucherId, TenVoucher, GiaTriApDung, CreatedAt, LastModified)
                                VALUES (@Id, @HoaDonId, @VoucherId, @TenVoucher, @GiaTri, @CreatedAt, @LastModified)
                            ", new
                            {
                                Id = Guid.NewGuid(),
                                HoaDonId = hoaDonId,
                                VoucherId = voucherId,
                                TenVoucher = tenVoucher,
                                GiaTri = Math.Abs(ct.ThanhTien),
                                CreatedAt = hd.NgayHoaDon ?? DateTime.Now,
                                LastModified = hd.NgayHoaDon ?? DateTime.Now
                            });
                        }
                        continue;
                    }

                    if (!spMapBase.TryGetValue(ct.IdSanPham, out var spId))
                    {
                        logMessages.Add($"⚠️ Hóa đơn {hd.IdHoaDon} - Không tìm thấy sản phẩm {ct.IdSanPham}");
                        continue;
                    }

                    var bienThe = spBienTheMap.FirstOrDefault(x => x.SpId == spId && Math.Abs(x.Gia - ct.DonGia) < 1);
                    if (bienThe.BienTheId == Guid.Empty)
                        bienThe = spBienTheMap.FirstOrDefault(x => x.SpId == spId && x.IsDefault);

                    Guid chiTietId = Guid.NewGuid();

                    await newDb.ExecuteAsync(@"
                        INSERT INTO ChiTietHoaDons(Id, HoaDonId, SanPhamBienTheId, TenBienThe, TenSanPham, SoLuong, DonGia, ThanhTien, NoteText, CreatedAt, LastModified)
                        VALUES (@Id, @HoaDonId, @BienTheId, @TenBienThe, @TenSP, @SL, @Gia, @TT, @GhiChu, @CreatedAt, @LastModified)
                    ", new
                    {
                        Id = chiTietId,
                        HoaDonId = hoaDonId,
                        BienTheId = bienThe.BienTheId,
                        TenBienThe = bienThe.Ten,
                        TenSP = ct.TenSanPham,
                        SL = ct.SoLuong,
                        Gia = ct.DonGia,
                        TT = ct.ThanhTien,
                        GhiChu = ct.GhiChu ?? "",
                        CreatedAt = hd.NgayHoaDon ?? DateTime.Now,
                        LastModified = hd.NgayHoaDon ?? DateTime.Now
                    });

                    string name = (ct.TenSanPham ?? "").ToLower();
                    foreach (var t in toppingMap)
                    {
                        if (name.Contains(t.Key))
                        {
                            int count = Regex.Matches(name, t.Key).Count;
                            int soLuongTopping = count > 0 ? count : 1;

                            await newDb.ExecuteAsync(@"
                                INSERT INTO ChiTietHoaDonToppings(Id, HoaDonId, ChiTietHoaDonId, ToppingId, TenTopping, SoLuong, Gia, CreatedAt, LastModified)
                                VALUES (@Id, @HoaDonId, @ChiTietHoaDonId, @ToppingId, @TenTopping, @SoLuong, @Gia, @CreatedAt, @LastModified)
                            ", new
                            {
                                Id = Guid.NewGuid(),
                                HoaDonId = hoaDonId,
                                ChiTietHoaDonId = chiTietId,
                                ToppingId = t.Value,
                                TenTopping = t.Key,
                                SoLuong = soLuongTopping,
                                Gia = ct.DonGia,
                                CreatedAt = hd.NgayHoaDon ?? DateTime.Now,
                                LastModified = hd.NgayHoaDon ?? DateTime.Now
                            });
                        }
                    }
                }

                added++;
                logMessages.Add($"✅ Đã import hóa đơn {hd.IdHoaDon}");
            }
            catch (Exception ex)
            {
                skipped++;
                logMessages.Add($"❌ Lỗi hóa đơn {hd.IdHoaDon}: {ex.Message}");
            }
        }

        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "import_log_full.txt");
        await File.WriteAllLinesAsync(logPath, logMessages);

        System.Windows.MessageBox.Show($"✅ Import xong! Thành công: {added}, Bỏ qua: {skipped}");
    }
}