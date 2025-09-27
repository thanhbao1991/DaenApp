// namespace TraSuaApp.Infrastructure.Helpers
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure;

public static class HoaDonHelper
{
    public static async Task RecalcConLaiAsync(AppDbContext db, Guid hoaDonId)
    {
        var h = await db.HoaDons
            .Include(x => x.ChiTietHoaDonNos.Where(n => !n.IsDeleted))
            .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(x => x.Id == hoaDonId);

        if (h == null) return;

        var sumNo = h.ChiTietHoaDonNos.Sum(n => n.SoTienConLai);
        var sumPaid = h.ChiTietHoaDonThanhToans.Sum(t => t.SoTien);

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
        //  await db.SaveChangesAsync();
    }
}