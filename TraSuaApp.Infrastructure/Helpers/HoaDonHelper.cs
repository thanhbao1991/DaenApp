using Microsoft.EntityFrameworkCore;
using TraSuaApp.Infrastructure;

public static class HoaDonHelper
{

    public static string ResolveTrangThai(
        decimal thanhTien,
        decimal conLai,
        bool hasDebt,
        bool coTienMat,
        bool coChuyenKhoan)
    {
        // 1) Không thu
        if (thanhTien == 0) return "Không thu";

        if (conLai < 0) conLai = 0;

        var daTra = thanhTien - conLai;

        // 2) Đã thu đủ
        if (conLai == 0)
        {
            if (coTienMat && coChuyenKhoan) return "Đã thu lẫn chuyển khoản"; // mixed
            if (!coTienMat && coChuyenKhoan) return "Đã chuyển khoản";        // only transfer
            return "Đã thu";                                                   // only cash/khác
        }

        // 3) Chưa thu đồng nào
        if (daTra <= 0) return hasDebt ? "Ghi nợ" : "Chưa thu";

        // 4) Thu một phần
        // Nếu chỉ có CK (không có tiền mặt) → "Chuyển khoản một phần"
        if (!coTienMat && coChuyenKhoan) return "Chuyển khoản một phần";

        // Còn lại dựa trên debt
        return hasDebt ? "Nợ một phần" : "Thu một phần";
    }

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