using Microsoft.EntityFrameworkCore;

namespace TraSuaApp.Infrastructure.Services;

public static class LoyaltyService
{
    // ✅ thêm ct, dùng biên [start, end) thay vì <= now
    public static async Task<(int diemThangNay, int diemThangTruoc)>
        TinhDiemThangAsync(AppDbContext db, Guid khachHangId, DateTime now, bool duocNhanVoucher, CancellationToken ct = default)
    {
        if (!duocNhanVoucher || khachHangId == Guid.Empty)
            return (-1, -1);

        var firstDayCurrent = new DateTime(now.Year, now.Month, 1);
        var firstDayPrev = firstDayCurrent.AddMonths(-1);
        var firstDayNext = firstDayCurrent.AddMonths(1);

        var agg = await db.ChiTietHoaDonPoints.AsNoTracking()
            .Where(p => !p.IsDeleted
                        && p.KhachHangId == khachHangId
                        && p.Ngay >= firstDayPrev
                        && p.Ngay < firstDayNext)
            .GroupBy(p => p.Ngay >= firstDayCurrent ? 1 : 0)
            .Select(g => new
            {
                IsCurrent = g.Key == 1,
                Sum = g.Sum(p => (int?)p.DiemThayDoi) ?? 0
            })
            .ToListAsync(ct);

        int diemThangNay = agg.Where(x => x.IsCurrent).Select(x => x.Sum).FirstOrDefault();
        int diemThangTruoc = agg.Where(x => !x.IsCurrent).Select(x => x.Sum).FirstOrDefault();
        return (diemThangNay, diemThangTruoc);
    }

    public static int TinhDiemTuHoaDon(decimal thanhTien)
        => (int)Math.Floor(thanhTien * 0.01m);

    public static async Task<bool> DaNhanVoucherTrongThangAsync(AppDbContext db, Guid khachHangId, DateTime now, CancellationToken ct = default)
    {
        var firstDayCurrent = new DateTime(now.Year, now.Month, 1);
        var firstDayNext = firstDayCurrent.AddMonths(1);

        return await (
            from v in db.ChiTietHoaDonVouchers.AsNoTracking()
            join h in db.HoaDons.AsNoTracking() on v.HoaDonId equals h.Id
            where h.KhachHangId == khachHangId
                  && !h.IsDeleted
                  && !v.IsDeleted
                  && v.CreatedAt >= firstDayCurrent
                  && v.CreatedAt < firstDayNext       // ✅ dùng [start, end)
            select v.Id
        ).AnyAsync(ct);
    }



    public static async Task<decimal> TinhTongDonKhacDangGiaoAsync(
    AppDbContext db, Guid khachHangId, Guid? excludeHoaDonId = null, CancellationToken ct = default)
    {
        return await db.HoaDons.AsNoTracking()
            .Where(h => !h.IsDeleted
                        && h.KhachHangId == khachHangId
                        && (excludeHoaDonId == null || h.Id != excludeHoaDonId)
                        && h.ConLai > 0
                        && !h.HasDebt
                        && h.Ngay >= DateTime.Now.Date
                        )
            .SumAsync(h => (decimal?)h.ConLai, ct) ?? 0m;
    }


    public static async Task<decimal> TinhTongNoKhachHangAsync(AppDbContext db, Guid khachHangId, Guid? excludeHoaDonId = null, CancellationToken ct = default)
    {
        var q = db.ChiTietHoaDonNos.AsNoTracking()
            .Where(h => !h.IsDeleted && h.KhachHangId == khachHangId);

        if (excludeHoaDonId.HasValue)
            q = q.Where(h => h.HoaDonId != excludeHoaDonId.Value);

        // chỉ cộng phần > 0, tránh null
        return await q.SumAsync(h => h.SoTienConLai > 0 ? h.SoTienConLai : 0m, ct);
    }
}