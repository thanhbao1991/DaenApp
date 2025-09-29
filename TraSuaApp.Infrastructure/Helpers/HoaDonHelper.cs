using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure;

public static class HoaDonHelper
{
    public static async Task RecalcConLaiAsync(AppDbContext db, Guid hoaDonId)
    {
        // Lấy entity TRONG TRACKING để cập nhật fields
        var h = await db.HoaDons.FirstOrDefaultAsync(x => x.Id == hoaDonId);
        if (h == null) return;

        // ✅ TÍNH LẠI trực tiếp từ DB, không dựa vào navigation đã cache
        var sumNo = await db.ChiTietHoaDonNos
            .Where(n => n.HoaDonId == hoaDonId && !n.IsDeleted)
            .SumAsync(n => (decimal?)n.SoTienConLai) ?? 0m;

        var sumPaid = await db.ChiTietHoaDonThanhToans
            .Where(t => t.HoaDonId == hoaDonId && !t.IsDeleted)
            .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

        if (sumNo > 0)
        {
            h.ConLai = sumNo;
            h.HasDebt = true;
        }
        else
        {
            h.ConLai = Math.Max(0m, h.ThanhTien - sumPaid);
            h.HasDebt = false;
        }

        h.LastModified = DateTime.Now;
    }
}