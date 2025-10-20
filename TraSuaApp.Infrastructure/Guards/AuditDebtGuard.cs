using System.Text;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.Infrastructure.Guards
{
    /// <summary>
    /// Phát hiện hóa đơn sai lệch giữa thanh toán - công nợ ngay tại thời điểm thao tác.
    /// KHÔNG cập nhật dữ liệu, chỉ gửi cảnh báo Discord.
    /// </summary>
    public static class AuditDebtGuard
    {
        /// <summary>
        /// Kiểm tra 1 hóa đơn sau khi thanh toán/cấn nợ, nếu phát hiện sai lệch thì báo Discord.
        /// </summary>
        public static async Task CheckAndNotifyAsync(AppDbContext db, Guid hoaDonId, string scope = "Audit")
        {
            var hd = await db.HoaDons
                .AsNoTracking()
                .Where(h => h.Id == hoaDonId)
                .Select(h => new
                {
                    h.Id,
                    h.MaHoaDon,
                    h.TenBan,
                    h.TenKhachHangText,
                    h.NgayGio,
                    h.ThanhTien,
                    h.ConLai,
                    h.HasDebt,
                    TongDaThu = db.ChiTietHoaDonThanhToans
                        .Where(t => !t.IsDeleted && t.HoaDonId == h.Id)
                        .Sum(t => (decimal?)t.SoTien) ?? 0m,
                    TongNoConLai = db.ChiTietHoaDonNos
                        .Where(n => !n.IsDeleted && n.HoaDonId == h.Id)
                        .Sum(n => (decimal?)n.SoTienConLai) ?? 0m
                })
                .FirstOrDefaultAsync();

            if (hd == null) return;

            // 🟟 Nếu hóa đơn đã thu >= tiền phải thu nhưng vẫn báo nợ hoặc còn công nợ
            if (hd.TongDaThu >= hd.ThanhTien &&
                (hd.TongNoConLai > 0 || hd.ConLai > 0 || hd.HasDebt))
            {
                var sb = new StringBuilder();
                sb.AppendLine("⚠️ **Phát hiện hóa đơn lệch giữa thanh toán và công nợ**");
                sb.AppendLine($"• Nguồn: `{scope}`");
                sb.AppendLine($"• KH: {hd.TenKhachHangText ?? hd.TenBan ?? "(Không rõ)"}");
                sb.AppendLine($"• Mã HĐ: `{hd.MaHoaDon}` ({hd.Id})");
                sb.AppendLine($"• Ngày giờ: {hd.NgayGio:yyyy-MM-dd HH:mm}");
                sb.AppendLine($"• ThanhTien: {hd.ThanhTien:N0}đ | Đã thu: {hd.TongDaThu:N0}đ");
                sb.AppendLine($"• NoConLai: {hd.TongNoConLai:N0}đ | ConLai(HĐ): {hd.ConLai:N0}đ | HasDebt: {(hd.HasDebt ? "1" : "0")}");
                sb.AppendLine($"• Thời điểm kiểm tra: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

                await DiscordService.SendAsync(DiscordEventType.Admin, sb.ToString());
            }
        }
    }
}