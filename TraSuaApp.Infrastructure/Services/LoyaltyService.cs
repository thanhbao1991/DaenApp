using Microsoft.EntityFrameworkCore;

namespace TraSuaApp.Infrastructure.Services;

public static class LoyaltyService
{
    public static int TinhDiemTuHoaDon(decimal thanhTien)
    {
        return (int)Math.Floor(thanhTien * 0.01m);
    }

    public static async Task<(int diemThangNay, int diemThangTruoc)>
        TinhDiemThangAsync(AppDbContext db, Guid khachHangId, DateTime now, bool duocNhanVoucher)
    {
        if (!duocNhanVoucher)
            return (-1, -1);

        var firstDayCurrent = new DateTime(now.Year, now.Month, 1);

        // Điểm tháng này
        int diemThangNay = await db.ChiTietHoaDonPoints.AsNoTracking()
            .Where(p => !p.IsDeleted && p.KhachHangId == khachHangId &&
                        p.Ngay >= firstDayCurrent &&
                        p.Ngay <= now.Date)
            .SumAsync(p => (int?)p.DiemThayDoi) ?? 0;

        // Điểm tháng trước
        var firstDayPrev = firstDayCurrent.AddMonths(-1);
        var lastDayPrev = firstDayCurrent.AddDays(-1);
        int diemThangTruoc = await db.ChiTietHoaDonPoints.AsNoTracking()
            .Where(p => !p.IsDeleted && p.KhachHangId == khachHangId &&
                        p.Ngay >= firstDayPrev &&
                        p.Ngay <= lastDayPrev)
            .SumAsync(p => (int?)p.DiemThayDoi) ?? 0;

        return (diemThangNay, diemThangTruoc);
    }

    public static async Task<bool> DaNhanVoucherTrongThangAsync(AppDbContext db, Guid khachHangId, DateTime now)
    {
        var firstDayCurrent = new DateTime(now.Year, now.Month, 1);

        return await (
            from v in db.ChiTietHoaDonVouchers.AsNoTracking()
            join h in db.HoaDons.AsNoTracking() on v.HoaDonId equals h.Id
            where h.KhachHangId == khachHangId
                  && !h.IsDeleted
                  && !v.IsDeleted
                  && v.CreatedAt >= firstDayCurrent
                  && v.CreatedAt <= now
            select v
        ).AnyAsync();
    }

    public static async Task<decimal> TinhTongNoKhachHangAsync(AppDbContext db, Guid khachHangId, Guid? excludeHoaDonId = null)
    {
        var congNoQuery = db.ChiTietHoaDonNos.AsNoTracking()
            .Where(h => h.KhachHangId == khachHangId && !h.IsDeleted);

        if (excludeHoaDonId.HasValue)
            congNoQuery = congNoQuery.Where(h => h.HoaDonId != excludeHoaDonId.Value);

        var result = await congNoQuery
.Select(h => new
{
    ConLai = h.SoTienConLai
})
            .ToListAsync();

        return result.Sum(x => x.ConLai > 0 ? x.ConLai : 0);
    }
}