using System.IO;
using Dapper;
using Microsoft.Data.SqlClient;
using TraSuaApp.Shared.Helpers;

public class OldHoaDon
{
    public int IdHoaDon { get; set; }
    public int? IdKhachHang { get; set; }
    public int? IdNhomHoaDon { get; set; }
    public int? IdBan { get; set; }
    public DateTime? NgayHoaDon { get; set; }
    public DateTime? NgayShip { get; set; }
    public DateTime? NgayRa { get; set; }
    public decimal TongTien { get; set; }
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


        var startOfJuly = new DateTime(DateTime.Today.Year, 7, 1);
        var endOfYear = new DateTime(DateTime.Today.Year, 12, 31).AddDays(1); // để lấy hết ngày 31/12

        var hoadons = (await oldDb.QueryAsync<OldHoaDon>(@"
    SELECT 
        h.NgayShip,
        h.IdHoaDon,
        h.NgayRa,
        h.IdKhachHang,
        h.IdNhomHoaDon,
        h.IdBan,
        h.NgayHoaDon,
        h.TongTien,
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
    WHERE h.IdNhomHoaDon < 20
      AND h.NgayHoaDon >= @startOfJuly
      AND h.NgayHoaDon < @endOfYear
    ORDER BY h.IdHoaDon
", new { startOfJuly, endOfYear })).ToList();


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
        // Map thủ công các sản phẩm Size L (DB cũ → DB mới)
        var manualSpMap = new Dictionary<int, int>
{
    { 5515, 43 },    // Bạc Xỉu Đá Size (Size L) -> Bạc Xỉu Đá
    { 1289, 184 },   // Cf Kemm (Size L) -> Cf Kemm
    { 8537, 3337 },  // Cf Muối (Size L) -> Cf Muối
    { 8554, 102 },   // Sinh Tố Sampo (Size L) -> Sinh Tố Sampo
    { 179, 100 },    // Sữa Tươi TCĐĐ (Size L) -> Sữa Tươi TCĐĐ
    { 5527, 1231 },  // Trà Sữa Khoai Môn (Size L) -> Trà Sữa Khoai Môn
    { 3451, 99 },    // Trà Sữa TCĐĐ (Size L) -> Trà Sữa TCĐĐ
    { 4511, 1241 },  // Trà Sữa Thái Xanh (Size L) -> Trà Sữa Thái Xanh
    { 5526, 96 },    // Trà Sữa Đenn (Size L) -> Trà Sữa Truyền Thống
};
        var spBienTheMap = (await newDb.QueryAsync<(Guid SpId, Guid BienTheId, decimal Gia, string Ten, bool IsDefault)>(@"
            SELECT bt.SanPhamId AS SpId, bt.Id AS BienTheId, bt.GiaBan AS Gia, bt.TenBienThe AS Ten,
                   CAST(ISNULL(bt.MacDinh, 0) AS BIT) AS IsDefault
            FROM SanPhamBienThes bt
        ")).ToList();

        var voucherMap = (await newDb.QueryAsync<(int OldId, Guid Id)>(@"
            SELECT OldId, Id FROM Vouchers
            WHERE OldId IS NOT NULL AND OldId <> 0
        ")).ToDictionary(x => x.OldId, x => x.Id);

        var toppingMap = (await newDb.QueryAsync<(int OldId, Guid Id)>(@"
            SELECT OldId, Id FROM Toppings
            WHERE OldId IS NOT NULL AND OldId <> 0
        ")).ToDictionary(x => x.OldId, x => x.Id);

        var phuongThucMap = (await newDb.QueryAsync<(string Ten, Guid Id)>(@"
            SELECT LOWER(Ten) AS Ten, Id FROM PhuongThucThanhToans
        ")).ToDictionary(x => x.Ten, x => x.Id);

        var nguyenLieuMap = (await newDb.QueryAsync<(int OldId, Guid NewId)>(@"
            SELECT OldId, Id FROM NguyenLieus
            WHERE OldId IS NOT NULL AND OldId <> 0
        ")).ToDictionary(x => x.OldId, x => x.NewId);

        int added = 0, skipped = 0;
        var logMessages = new List<string>();

        foreach (var hd in hoadons)
        {
            if (hd.IdNhomHoaDon < 10)
            {
                try
                {
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

                    // Xác định trạng thái hóa đơn
                    //string trangThai;
                    //var ngayHoaDon = (hd.NgayHoaDon ?? DateTime.Now).Date;

                    //if (hd.DaThu >= hd.TongTien)
                    //{
                    //    trangThai = "Đã Thu";
                    //}
                    //else if (hd.DaThu == 0 && hd.ConLai > 0)
                    //{
                    //    // Nếu hóa đơn hôm nay chưa thu đồng nào => Chưa Thu
                    //    if (hd.NgayRa == null)
                    //        trangThai = "Chưa Thu";
                    //    else
                    //        trangThai = "Ghi Nợ"; // bán chịu từ trước
                    //}
                    //else if (hd.DaThu > 0 && hd.ConLai > 0)
                    //{
                    //    trangThai = "Còn Nợ"; // thu một phần
                    //}
                    //else
                    //{
                    //    trangThai = "Khác";
                    //}
                    // Tạo ghi chú tóm tắt sản phẩm
                    var ghiChuTomTat = string.Join(", ",
                        cts
                        .Where(x => !voucherMap.ContainsKey(x.IdSanPham) && !toppingMap.ContainsKey(x.IdSanPham))
                        .GroupBy(x => x.TenSanPham.Trim())
                        .Select(g => $"{g.Sum(x => x.SoLuong)} {g.Key}")
                    );

                    await newDb.ExecuteAsync(@"
                    INSERT INTO HoaDons(TenKhachHangText, GhiChu, NgayShip, NgayRa, Id, OldId, MaHoaDon, PhanLoai, TenBan, DiaChiText, SoDienThoaiText,
                                        KhachHangId, TongTien, GiamGia, ThanhTien, Ngay, NgayGio, CreatedAt, LastModified, IsDeleted)
                    VALUES (@TenKhachHangText, @GhiChu, @NgayShip, @NgayRa, @Id, @OldId, @MaHoaDon, @PhanLoai, @TenBan, @DiaChi, @Phone,
                            @KhId, @TongTien, @GiamGia, @ThanhTien, @Ngay, @NgayGio, @CreatedAt, @LastModified, 0)
                ", new
                    {
                        TenKhachHangText = hd.ThongTinHoaDon,
                        GhiChu = ghiChuTomTat,
                        Id = hoaDonId,
                        NgayShip = hd.NgayShip,
                        NgayRa = hd.NgayRa,
                        OldId = hd.IdHoaDon,
                        MaHoaDon = $"HD{hd.IdHoaDon:D6}",
                        //TrangThai = trangThai,
                        PhanLoai = phanLoai,
                        TenBan = tenBan,
                        DiaChi = hd.DiaChiShip ?? "",
                        Phone = hd.DienThoaiShip ?? "",
                        KhId = khId,
                        TongTien = cts.Where(x => !voucherMap.ContainsKey(x.IdSanPham) && !toppingMap.ContainsKey(x.IdSanPham)).Sum(x => x.ThanhTien) +
                                   cts.Where(x => toppingMap.ContainsKey(x.IdSanPham)).Sum(x => x.DonGia * x.SoLuong),
                        GiamGia = cts.Where(x => voucherMap.ContainsKey(x.IdSanPham)).Sum(x => Math.Abs(x.ThanhTien)),
                        ThanhTien = hd.TongTien,
                        Ngay = (hd.NgayHoaDon ?? DateTime.Now).Date,
                        NgayGio = hd.NgayHoaDon ?? DateTime.Now,
                        CreatedAt = hd.NgayHoaDon ?? DateTime.Now,
                        LastModified = hd.NgayHoaDon ?? DateTime.Now
                    });



                    // === Thanh toán, Nợ, Điểm (giữ nguyên) ===
                    decimal tienMat = hd.DaThu - hd.TienBank;
                    if (tienMat > 0 && phuongThucMap.TryGetValue("tiền mặt", out var tienMatId))
                    {
                        await newDb.ExecuteAsync(@"
    INSERT INTO ChiTietHoaDonThanhToans
    (TenPhuongThucThanhToan, GhiChu, Id, HoaDonId, PhuongThucThanhToanId, SoTien, Ngay, NgayGio, CreatedAt, LastModified, KhachHangId, LoaiThanhToan)
    VALUES (@TenPhuongThucThanhToan, @GhiChu, @Id, @HoaDonId, @PTTT, @SoTien, @Ngay, @NgayGio, @CreatedAt, @LastModified, @KhId, @LoaiThanhToan)
", new
                        {
                            TenPhuongThucThanhToan = "Tiền mặt",
                            GhiChu = "",
                            Id = Guid.NewGuid(),
                            HoaDonId = hoaDonId,
                            PTTT = tienMatId,
                            SoTien = tienMat,
                            LoaiThanhToan = "Trong ngày",
                            Ngay = (hd.NgayHoaDon ?? DateTime.Now).Date,
                            NgayGio = hd.NgayHoaDon ?? DateTime.Now,
                            CreatedAt = hd.NgayHoaDon ?? DateTime.Now,
                            LastModified = hd.NgayHoaDon ?? DateTime.Now,
                            KhId = khId
                        });
                    }

                    if (hd.TienBank > 0 && phuongThucMap.TryGetValue("chuyển khoản", out var ckId))
                    {
                        await newDb.ExecuteAsync(@"
    INSERT INTO ChiTietHoaDonThanhToans
    (TenPhuongThucThanhToan, GhiChu, Id, HoaDonId, PhuongThucThanhToanId, SoTien, Ngay, NgayGio, CreatedAt, LastModified, KhachHangId,LoaiThanhToan)
    VALUES (@TenPhuongThucThanhToan, @GhiChu, @Id, @HoaDonId, @PTTT, @SoTien, @Ngay, @NgayGio, @CreatedAt, @LastModified, @KhId,@LoaiThanhToan)
", new
                        {
                            TenPhuongThucThanhToan = "Chuyển khoản",
                            LoaiThanhToan = "Trong ngày",
                            GhiChu = "",
                            Id = Guid.NewGuid(),
                            HoaDonId = hoaDonId,
                            PTTT = ckId,
                            SoTien = hd.TienBank,
                            Ngay = (hd.NgayHoaDon ?? DateTime.Now).Date,
                            NgayGio = hd.NgayHoaDon ?? DateTime.Now,
                            CreatedAt = hd.NgayHoaDon ?? DateTime.Now,
                            LastModified = hd.NgayHoaDon ?? DateTime.Now,
                            KhId = khId
                        });
                    }

                    if (hd.TienNo > 0 && hd.ConLai > 0)
                    {
                        await newDb.ExecuteAsync(@"
    INSERT INTO ChiTietHoaDonNos
    (GhiChu,Id, HoaDonId, KhachHangId, SoTienNo, Ngay, NgayGio, CreatedAt, LastModified)
    VALUES (@GhiChu,@Id, @HoaDonId, @KhId, @SoTienNo, @Ngay, @NgayGio, @CreatedAt, @LastModified)
", new
                        {
                            GhiChu = ghiChuTomTat,
                            Id = Guid.NewGuid(),
                            HoaDonId = hoaDonId,
                            KhId = khId,
                            SoTienNo = hd.ConLai,
                            Ngay = (hd.NgayNo ?? hd.NgayHoaDon ?? DateTime.Now).Date,
                            NgayGio = hd.NgayNo ?? hd.NgayHoaDon ?? DateTime.Now,
                            CreatedAt = hd.NgayHoaDon ?? DateTime.Now,
                            LastModified = hd.NgayHoaDon ?? DateTime.Now
                        });
                    }

                    if (khId.HasValue)
                    {
                        int diemTichLuy = (int)Math.Floor(hd.TongTien * 0.01m);
                        DateTime ngayGio = hd.NgayHoaDon ?? DateTime.Now;
                        string maHoaDon = MaHoaDonGenerator.Generate();

                        await newDb.ExecuteAsync(@"
        INSERT INTO ChiTietHoaDonPoints
        (Id,HoaDonId, KhachHangId, Ngay, NgayGio, DiemThayDoi, GhiChu, CreatedAt, LastModified)
        VALUES (@Id,@HoaDonId, @KhachHangId, @Ngay, @NgayGio, @DiemThayDoi, @GhiChu, @CreatedAt, @LastModified)
    ", new
                        {
                            Id = Guid.NewGuid(),
                            HoaDonId = hoaDonId,
                            KhachHangId = khId.Value,
                            Ngay = ngayGio.Date,
                            NgayGio = ngayGio,
                            DiemThayDoi = diemTichLuy,
                            GhiChu = $"Tích điểm từ mã hóa đơn {maHoaDon}",
                            CreatedAt = ngayGio,
                            LastModified = ngayGio
                        });

                    }
                    // === Insert chi tiết hóa đơn ===
                    foreach (var ct in cts)
                    {
                        // Voucher
                        if (voucherMap.TryGetValue(ct.IdSanPham, out var voucherId))
                        {
                            await newDb.ExecuteAsync(@"
                            INSERT INTO ChiTietHoaDonVouchers(Id, HoaDonId, VoucherId, TenVoucher, GiaTriApDung, CreatedAt, LastModified)
                            VALUES (@Id, @HoaDonId, @VoucherId, @TenVoucher, @GiaTri, @CreatedAt, @LastModified)
                        ", new
                            {
                                Id = Guid.NewGuid(),
                                HoaDonId = hoaDonId,
                                VoucherId = voucherId,
                                TenVoucher = ct.TenSanPham,
                                GiaTri = Math.Abs(ct.ThanhTien),
                                CreatedAt = hd.NgayHoaDon ?? DateTime.Now,
                                LastModified = hd.NgayHoaDon ?? DateTime.Now
                            });
                            continue;
                        }

                        // Topping
                        if (toppingMap.TryGetValue(ct.IdSanPham, out var toppingId))
                        {
                            await newDb.ExecuteAsync(@"
                            INSERT INTO ChiTietHoaDonToppings(Id, HoaDonId, ChiTietHoaDonId, ToppingId, TenTopping, SoLuong, Gia, CreatedAt, LastModified)
                            VALUES (@Id, @HoaDonId, @ChiTietHoaDonId, @ToppingId, @TenTopping, @SoLuong, @Gia, @CreatedAt, @LastModified)
                        ", new
                            {
                                Id = Guid.NewGuid(),
                                HoaDonId = hoaDonId,
                                ChiTietHoaDonId = Guid.NewGuid(), // gắn tạm, cần map chi tiết sp nếu cần
                                ToppingId = toppingId,
                                TenTopping = ct.TenSanPham,
                                SoLuong = ct.SoLuong,
                                Gia = ct.DonGia,
                                CreatedAt = hd.NgayHoaDon ?? DateTime.Now,
                                LastModified = hd.NgayHoaDon ?? DateTime.Now
                            });
                            continue;
                        }

                        // Sản phẩm chính
                        // Sản phẩm chính
                        int spOldId = ct.IdSanPham;

                        // Map thủ công từ DB cũ sang DB mới (nếu có)
                        if (manualSpMap.TryGetValue(spOldId, out var newSpOldId))
                            spOldId = newSpOldId;

                        // Lấy ra Id sản phẩm gốc bên DB mới
                        if (!spMapBase.TryGetValue(spOldId, out var spId))
                        {
                            logMessages.Add($"⚠️ Hóa đơn {hd.IdHoaDon} - Không tìm thấy sản phẩm {ct.IdSanPham} {ct.TenSanPham}");
                            continue;
                        }

                        // ===== Tìm biến thể phù hợp =====
                        var bienThe = spBienTheMap.FirstOrDefault(
                            x => x.SpId == spId && Math.Abs(x.Gia - ct.DonGia) < 1
                        );

                        // Nếu không match theo giá → thử check theo tên "size L"
                        if (bienThe.BienTheId == Guid.Empty)
                        {
                            var tenLower = (ct.TenSanPham ?? "").ToLower();
                            if (tenLower.Contains("size l"))
                            {
                                bienThe = spBienTheMap.FirstOrDefault(
                                    x => x.SpId == spId && x.Ten.ToLower().Contains("l")
                                );
                            }
                        }

                        // Nếu vẫn chưa có → fallback về biến thể mặc định
                        if (bienThe.BienTheId == Guid.Empty)
                            bienThe = spBienTheMap.FirstOrDefault(x => x.SpId == spId && x.IsDefault);

                        // Nếu vẫn không ra → log cảnh báo và bỏ qua
                        if (bienThe.BienTheId == Guid.Empty)
                        {
                            logMessages.Add($"⚠️ Hóa đơn {hd.IdHoaDon} - Không tìm thấy biến thể cho sản phẩm {ct.IdSanPham} {ct.TenSanPham}");
                            continue;
                        }

                        // === Insert chi tiết hóa đơn ===
                        Guid chiTietId = Guid.NewGuid();
                        await newDb.ExecuteAsync(@"
    INSERT INTO ChiTietHoaDons
    (Id, HoaDonId, SanPhamBienTheId, TenBienThe, TenSanPham, SoLuong, DonGia, ThanhTien, NoteText, CreatedAt, LastModified)
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

                    }

                    added++;
                    //logMessages.Add($"✅ Đã import hóa đơn {hd.IdHoaDon}");
                }
                catch (Exception ex)
                {
                    skipped++;
                    logMessages.Add($"❌ Lỗi hóa đơn {hd.IdHoaDon}: {ex.Message}");
                }
            }
            else
            {
                if (hd.IdNhomHoaDon > 10 && hd.IdNhomHoaDon < 20)
                {
                    var cts = chitiets.Where(c => c.IdHoaDon == hd.IdHoaDon).OrderBy(c => c.IdChiTietHoaDon).ToList();
                    foreach (var ct in cts)
                    {
                        if (!nguyenLieuMap.TryGetValue(ct.IdSanPham, out var nlId))
                        {
                            logMessages.Add($"⚠️ Chi tiêu {hd.IdHoaDon} - Không tìm thấy nguyên liệu {ct.IdSanPham}");
                            continue;
                        }

                        await newDb.ExecuteAsync(@"
                            INSERT INTO ChiTieuHangNgays
                            (Id, Ten, NguyenLieuId, SoLuong, DonGia, ThanhTien, Ngay, NgayGio, GhiChu, CreatedAt, LastModified, BillThang,IsDeleted)
                            VALUES (@Id,@Ten, @NguyenLieuId, @SoLuong, @DonGia, @ThanhTien, @Ngay, @NgayGio, @GhiChu, @CreatedAt, @LastModified, @BillThang,@IsDeleted)
                        ", new
                        {
                            Id = Guid.NewGuid(),
                            Ten = ct.TenSanPham,
                            NguyenLieuId = nlId,
                            SoLuong = ct.SoLuong,
                            DonGia = ct.DonGia,
                            ThanhTien = ct.ThanhTien,
                            Ngay = (hd.NgayHoaDon ?? DateTime.Now).Date,
                            NgayGio = (hd.NgayHoaDon ?? DateTime.Now),
                            GhiChu = ct.GhiChu ?? "",
                            CreatedAt = hd.NgayHoaDon ?? DateTime.Now,
                            LastModified = hd.NgayHoaDon ?? DateTime.Now,
                            BillThang = hd.IdBan == 1051 ? true : false,
                            IsDeleted = false,
                        });
                    }
                    // logMessages.Add($"✅ Đã import chi tiêu {hd.IdHoaDon}");
                }


            }
        }

        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "import_log_full.txt");
        await File.WriteAllLinesAsync(logPath, logMessages);

        System.Windows.MessageBox.Show($"✅ Import xong! Thành công: {added}, Bỏ qua: {skipped}");
    }
}
