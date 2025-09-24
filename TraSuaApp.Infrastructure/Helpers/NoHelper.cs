using Microsoft.EntityFrameworkCore;

namespace TraSuaApp.Infrastructure.Helpers;

public static class NoHelper
{
    public static async Task UpdateSoTienConLaiAsync(AppDbContext db, Guid? chiTietNoId, decimal delta)
    {
        if (chiTietNoId == null) return;

        var no = await db.ChiTietHoaDonNos
            .FirstOrDefaultAsync(x => x.Id == chiTietNoId && !x.IsDeleted);

        if (no != null)
        {
            no.SoTienConLai += delta;
            if (no.SoTienConLai < 0) no.SoTienConLai = 0;
            no.LastModified = DateTime.Now;
        }
    }
}