// Infrastructure/Services/ShipperService.cs
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Applicationn.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Guards;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Services;

namespace TraSuaApp.Infrastructure.Services
{
    public class ShipperService : IShipperService
    {
        private readonly AppDbContext _context;

        public ShipperService(AppDbContext context)
        {
            _context = context;
        }

        private HoaDonDto ToDto(HoaDon entity)
        {
            var pays = entity.ChiTietHoaDonThanhToans?.Where(t => !t.IsDeleted).ToList() ?? new List<ChiTietHoaDonThanhToan>();
            bool coTienMat = pays.Any(t => (t.TenPhuongThucThanhToan ?? "").Contains("Tiền mặt"));
            bool coChuyenKhoan = pays.Any(t => (t.TenPhuongThucThanhToan ?? "").Contains("Chuyển khoản"));

            var trangThai = HoaDonHelper.ResolveTrangThai(entity.ThanhTien, entity.ConLai, entity.HasDebt, coTienMat, coChuyenKhoan);

            return new HoaDonDto
            {
                Id = entity.Id,
                MaHoaDon = entity.MaHoaDon,
                Ngay = entity.Ngay,
                BaoDon = entity.BaoDon,
                UuTien = entity.UuTien,
                NgayGio = entity.NgayGio,
                NgayShip = entity.NgayShip,
                NguoiShip = entity.NguoiShip,
                NgayHen = entity.NgayHen,
                NgayRa = entity.NgayRa,
                PhanLoai = entity.PhanLoai,
                TenBan = entity.TenBan,
                TenKhachHangText = !string.IsNullOrWhiteSpace(entity.TenKhachHangText) ? entity.TenKhachHangText : entity.TenBan,
                DiaChiText = entity.DiaChiText,
                SoDienThoaiText = entity.SoDienThoaiText,
                VoucherId = entity.VoucherId,
                KhachHangId = entity.KhachHangId,
                TongTien = entity.TongTien,
                GiamGia = entity.GiamGia,
                ThanhTien = entity.ThanhTien,
                GhiChu = entity.GhiChu,
                GhiChuShipper = entity.GhiChuShipper,
                CreatedAt = entity.CreatedAt,
                LastModified = entity.LastModified,
                ConLai = entity.ConLai,
                TrangThai = trangThai
            };
        }
        private static string ResolveLoaiThanhToan(HoaDon entity, DateTime now)
        {
            // So sánh theo Date để bỏ giờ
            if (entity.Ngay.Date == now.Date)
                return "Trong ngày";

            return "Trả nợ qua ngày";
        }
        public async Task<List<HoaDonDto>> GetForShipperAsync(DateTime? day = null, string? shipper = "Khánh")
        {
            var start = (day?.Date ?? DateTime.Today);
            var end = start.AddDays(1);
            var shipperName = string.IsNullOrWhiteSpace(shipper) ? "Khánh" : shipper.Trim();

            var list = await _context.HoaDons.AsNoTracking()
                .Where(x => !x.IsDeleted
                    && x.PhanLoai == "Ship"
                    && x.Ngay >= start && x.Ngay < end
                    && x.NgayShip != null
                    && x.NguoiShip == shipperName)
                .OrderByDescending(x => x.NgayGio)
                .Select(x => new
                {
                    x.Id,
                    x.TenKhachHangText,
                    x.DiaChiText,
                    x.SoDienThoaiText,
                    x.ThanhTien,
                    x.GhiChu,
                    x.GhiChuShipper,
                    x.HasDebt,
                    x.ConLai,
                    x.NgayGio,
                    x.NgayShip,
                    x.NguoiShip,
                    x.KhachHangId
                })
                .ToListAsync();

            var result = new List<HoaDonDto>();
            foreach (var h in list)
            {
                var dto = new HoaDonDto
                {
                    Id = h.Id,
                    TenKhachHangText = h.TenKhachHangText,
                    DiaChiText = h.DiaChiText,
                    SoDienThoaiText = h.SoDienThoaiText,
                    ThanhTien = h.ThanhTien,
                    ConLai = h.ConLai,
                    HasDebt = false, // không cần ở view này
                    NgayGio = h.NgayGio,
                    NgayShip = h.NgayShip,
                    NguoiShip = h.NguoiShip,
                    GhiChu = h.GhiChu,
                    GhiChuShipper = h.GhiChuShipper,
                };

                if (h.KhachHangId != null)
                {
                    dto.TongNoKhachHang = await LoyaltyService
                        .TinhTongNoKhachHangAsync(_context, h.KhachHangId.Value, h.Id);
                    dto.TongDonKhacDangGiao = await LoyaltyService
                           .TinhTongDonKhacDangGiaoAsync(_context, h.KhachHangId.Value, h.Id);

                }

                result.Add(dto);
            }

            return result;
        }

        public async Task<Result<HoaDonDto>> TiNuaChuyenKhoanAsync(Guid id)
        {
            var entity = await _context.HoaDons
              .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");
            if (IsFinalizedByShipper(entity))
                return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");
            var now = DateTime.Now;
            var before = ToDto(entity);

            decimal daThu = await _context.ChiTietHoaDonThanhToans
        .Where(t => !t.IsDeleted && t.HoaDonId == entity.Id)
        .SumAsync(t => (decimal?)t.SoTien) ?? 0;

            decimal conLai = entity.ThanhTien - daThu;
            entity.GhiChuShipper = $"Tí nữa chuyển khoản: {conLai:N0} đ";
            entity.LastModified = now;

            await _context.SaveChangesAsync();
            await HoaDonHelper.RecalcConLaiAsync(_context, entity.Id);
            await _context.SaveChangesAsync();

            var after = ToDto(entity);

            await DiscordService.SendAsync(
                DiscordEventType.DuyKhanh,
                $"{entity.TenKhachHangText} {entity.GhiChuShipper}"
            );

            return Result<HoaDonDto>.Success(after, "Cập nhật hóa đơn thành công.")
                .WithId(id)
                .WithBefore(before)
                .WithAfter(after);
        }

        public async Task<Result<HoaDonDto>> TraNoAsync(Guid id, decimal soTienKhachDua)
        {
            var entity = await _context.HoaDons
                .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
                .Include(x => x.ChiTietHoaDonNos.Where(n => !n.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

            var now = DateTime.Now;
            var before = ToDto(entity);

            if (entity.KhachHangId == null)
                return Result<HoaDonDto>.Failure("Hoá đơn này không có khách hàng, không thể trả nợ.");

            decimal soTienThucTe = soTienKhachDua * 1000;

            decimal daThuHomNay = entity.ChiTietHoaDonThanhToans.Where(t => t.GhiChu == "Shipper").Sum(t => t.SoTien);
            decimal soTienTraNo = soTienThucTe - daThuHomNay;
            if (soTienTraNo <= 0)
                return Result<HoaDonDto>.Failure("Khách không đưa dư sau phần đã thu của đơn hôm nay.");

            var khId = entity.KhachHangId.Value;

            var tongNoCu = await LoyaltyService.TinhTongNoKhachHangAsync(_context, khId, entity.Id);
            if (tongNoCu <= 0)
                return Result<HoaDonDto>.Failure("Khách hàng không còn nợ để trả.");

            decimal soTienCon = Math.Min(soTienTraNo, tongNoCu);
            decimal traNoCu = 0;

            var pm = await _context.PhuongThucThanhToans
                .Where(p => !p.IsDeleted && p.Ten == "Tiền mặt")
                .Select(p => new { p.Id, p.Ten })
                .FirstOrDefaultAsync();

            if (pm == null)
                return Result<HoaDonDto>.Failure("Không tìm thấy phương thức thanh toán 'Tiền mặt'.");

            var noConLaiList = await _context.ChiTietHoaDonNos
                .Where(n => !n.IsDeleted
                         && n.KhachHangId == khId
                         && n.HoaDonId != entity.Id
                         && n.SoTienConLai > 0)
                .OrderBy(n => n.NgayGio)
                .ToListAsync();
            var affectedInvoiceIds = new HashSet<Guid>();

            foreach (var n in noConLaiList)
            {
                var soNoCon = n.SoTienConLai;
                if (soNoCon <= 0) continue;

                var tra = Math.Min(soTienCon, soNoCon);
                if (tra <= 0) break;

                _context.ChiTietHoaDonThanhToans.Add(new ChiTietHoaDonThanhToan
                {
                    Id = Guid.NewGuid(),
                    HoaDonId = n.HoaDonId,
                    KhachHangId = khId,
                    Ngay = now.Date,
                    NgayGio = now,
                    SoTien = tra,
                    LoaiThanhToan = n.Ngay == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày",
                    PhuongThucThanhToanId = pm.Id,
                    TenPhuongThucThanhToan = pm.Ten,
                    GhiChu = "Shipper",
                    CreatedAt = now,
                    LastModified = now,
                    IsDeleted = false,
                    ChiTietHoaDonNoId = n.Id
                });

                await NoHelper.UpdateSoTienConLaiAsync(_context, n.Id, -tra);
                affectedInvoiceIds.Add(n.HoaDonId);

                traNoCu += tra;
                soTienCon -= tra;
                if (soTienCon <= 0) break;
            }

            var ghiChuCu = string.IsNullOrWhiteSpace(entity.GhiChuShipper) ? "" : entity.GhiChuShipper + " | ";
            entity.GhiChuShipper = $"{ghiChuCu}Trả nợ: {traNoCu:N0} đ";
            entity.LastModified = now;

            await _context.SaveChangesAsync();

            foreach (var hid in affectedInvoiceIds)
            {
                await HoaDonHelper.RecalcConLaiAsync(_context, hid);
            }
            await HoaDonHelper.RecalcConLaiAsync(_context, entity.Id);

            await _context.SaveChangesAsync();
            var after = ToDto(entity);

            await DiscordService.SendAsync(DiscordEventType.DuyKhanh, $"{entity.TenKhachHangText} {entity.GhiChuShipper}");

            return Result<HoaDonDto>.Success(after, "Đã ghi nhận khách trả nợ.")
                .WithId(entity.Id)
                .WithBefore(before)
                .WithAfter(after);
        }

        public async Task<Result<HoaDonDto>> GhiNoAsync(Guid id)
        {
            var entity = await _context.HoaDons
                .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
                .Include(x => x.ChiTietHoaDonNos.Where(n => !n.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");
            if (IsFinalizedByShipper(entity))
                return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");

            if (entity.KhachHangId == null)
                return Result<HoaDonDto>.Failure("Hóa đơn này chưa gắn khách, không thể ghi nợ.");

            var now = DateTime.Now;
            var before = ToDto(entity);

            var daThu = entity.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Sum(t => t.SoTien);
            var soTienNo = entity.ThanhTien - daThu;

            if (soTienNo > 0 && !entity.ChiTietHoaDonNos.Any(x => !x.IsDeleted))
            {
                var no = new ChiTietHoaDonNo
                {
                    Id = Guid.NewGuid(),
                    HoaDonId = entity.Id,
                    KhachHangId = entity.KhachHangId ?? Guid.Empty,
                    Ngay = now.Date,
                    NgayGio = now,
                    SoTienNo = soTienNo,
                    SoTienConLai = soTienNo,
                    GhiChu = entity.GhiChu,
                    CreatedAt = now,
                    LastModified = now,
                    IsDeleted = false
                };
                _context.ChiTietHoaDonNos.Add(no);
            }

            entity.GhiChuShipper = $"Ghi nợ: {soTienNo:N0} đ";
            entity.LastModified = now;

            await _context.SaveChangesAsync();
            await HoaDonHelper.RecalcConLaiAsync(_context, entity.Id);
            await _context.SaveChangesAsync();

            var after = ToDto(entity);

            await DiscordService.SendAsync(DiscordEventType.DuyKhanh, $"{entity.TenKhachHangText} {entity.GhiChuShipper}");

            return Result<HoaDonDto>.Success(after, "Đã ghi nợ cho hóa đơn.")
                .WithId(id).WithBefore(before).WithAfter(after);
        }

        public async Task<Result<HoaDonDto>> ThuTienMatAsync(Guid id)
        {
            var entity = await _context.HoaDons
                .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
                .Include(x => x.ChiTietHoaDonNos.Where(n => !n.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return Result<HoaDonDto>.Failure("Không tìm thấy hoá đơn.");

            // NEW: Chặn nếu đã có thanh toán TIỀN MẶT từ shipper cho đơn này
            if (entity.ChiTietHoaDonThanhToans.Any(t =>
                    !t.IsDeleted
                    && t.GhiChu == "Shipper"
                    && t.TenPhuongThucThanhToan == "Tiền mặt"))
            {
                return Result<HoaDonDto>.Failure("Shipper đã thu TIỀN MẶT cho đơn này, không thể thu lại.");
            }

            // Guard cũ: nếu đã có ghi chú shipper thì không cho thao tác nữa
            // Nếu đã có ghi chú “chốt đơn” (trừ “Tí nữa chuyển khoản”) thì khóa lại
            if (IsFinalizedByShipper(entity))
                return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");
            // Nếu đã ghi nợ và còn nợ thì không được dùng Thu tiền mặt
            if (entity.ChiTietHoaDonNos.Any(n => !n.IsDeleted && n.SoTienConLai > 0))
                return Result<HoaDonDto>.Failure("Hoá đơn đã ghi nợ, vui lòng dùng chức năng Trả nợ.");

            var now = DateTime.Now;
            var before = ToDto(entity);

            var daThu = entity.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Sum(t => t.SoTien);
            var soTienThu = entity.ThanhTien - daThu;

            // NEW: chặn luôn nếu đã thu đủ
            if (soTienThu <= 0)
            {
                return Result<HoaDonDto>.Failure("Hoá đơn đã thu đủ tiền, không thể thu thêm.");
            }

            if (soTienThu > 0)
            {
                var pm = await _context.PhuongThucThanhToans
                    .Where(p => !p.IsDeleted && p.Ten == "Tiền mặt")
                    .Select(p => new { p.Id, p.Ten })
                    .FirstOrDefaultAsync();

                if (pm == null)
                    return Result<HoaDonDto>.Failure("Không tìm thấy phương thức thanh toán 'Tiền mặt'.");

                //bool daCoNo = entity.ChiTietHoaDonNos.Any(n => !n.IsDeleted);
                //var loai = daCoNo
                //    ? (entity.Ngay == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày")
                //    : "Trong ngày";
                var loai = ResolveLoaiThanhToan(entity, now);

                var thanhToan = new ChiTietHoaDonThanhToan
                {
                    Id = Guid.NewGuid(),
                    HoaDonId = entity.Id,
                    KhachHangId = entity.KhachHangId,
                    Ngay = now.Date,
                    NgayGio = now,
                    SoTien = soTienThu,
                    LoaiThanhToan = loai,
                    PhuongThucThanhToanId = pm.Id,
                    TenPhuongThucThanhToan = pm.Ten,
                    GhiChu = "Shipper",
                    CreatedAt = now,
                    LastModified = now,
                    IsDeleted = false,
                    ChiTietHoaDonNoId = null
                };

                _context.ChiTietHoaDonThanhToans.Add(thanhToan);
            }

            entity.GhiChuShipper = $"Tiền mặt: {soTienThu:N0} đ";
            entity.LastModified = now;

            await _context.SaveChangesAsync();
            await HoaDonHelper.RecalcConLaiAsync(_context, entity.Id);
            await _context.SaveChangesAsync();
            await AuditDebtGuard.CheckAndNotifyAsync(_context, entity.Id, "CreatePayment");

            var after = ToDto(entity);
            return Result<HoaDonDto>.Success(after, "Đã thu tiền mặt.")
                .WithId(id).WithBefore(before).WithAfter(after);
        }

        public async Task<Result<HoaDonDto>> ThuChuyenKhoanAsync(Guid id)
        {
            var entity = await _context.HoaDons
                .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
                .Include(x => x.ChiTietHoaDonNos.Where(n => !n.IsDeleted))
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null)
                return Result<HoaDonDto>.Failure("Không tìm thấy hoá đơn.");

            // NEW: Chặn nếu đã có thanh toán CHUYỂN KHOẢN từ shipper cho đơn này
            if (entity.ChiTietHoaDonThanhToans.Any(t =>
                    !t.IsDeleted
                    && t.GhiChu == "Shipper"
                    && t.TenPhuongThucThanhToan == "Chuyển khoản"))
            {
                return Result<HoaDonDto>.Failure("Shipper đã thu CHUYỂN KHOẢN cho đơn này, không thể thu lại.");
            }

            if (IsFinalizedByShipper(entity))
                return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");
            if (entity.ChiTietHoaDonNos.Any(n => !n.IsDeleted && n.SoTienConLai > 0))
                return Result<HoaDonDto>.Failure("Hoá đơn đã ghi nợ, vui lòng dùng chức năng Trả nợ.");

            var now = DateTime.Now;
            var before = ToDto(entity);

            var daThu = entity.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Sum(t => t.SoTien);
            var soTienThu = entity.ThanhTien - daThu;

            // NEW: chặn nếu đã thu đủ
            if (soTienThu <= 0)
            {
                return Result<HoaDonDto>.Failure("Hoá đơn đã thu đủ tiền, không thể thu thêm.");
            }

            if (soTienThu > 0)
            {
                var pm = await _context.PhuongThucThanhToans
                    .Where(p => !p.IsDeleted && p.Ten == "Chuyển khoản")
                    .Select(p => new { p.Id, p.Ten })
                    .FirstOrDefaultAsync();

                if (pm == null)
                    return Result<HoaDonDto>.Failure("Không tìm thấy phương thức thanh toán 'Chuyển khoản'.");

                //bool daCoNo = entity.ChiTietHoaDonNos.Any(n => !n.IsDeleted);
                //var loai = daCoNo
                //    ? (entity.Ngay == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày")
                //    : "Trong ngày";
                var loai = ResolveLoaiThanhToan(entity, now);

                var thanhToan = new ChiTietHoaDonThanhToan
                {
                    Id = Guid.NewGuid(),
                    HoaDonId = entity.Id,
                    KhachHangId = entity.KhachHangId,
                    Ngay = now.Date,
                    NgayGio = now,
                    SoTien = soTienThu,
                    LoaiThanhToan = loai,
                    PhuongThucThanhToanId = pm.Id,
                    TenPhuongThucThanhToan = pm.Ten,
                    GhiChu = "Shipper",
                    CreatedAt = now,
                    LastModified = now,
                    IsDeleted = false,
                    ChiTietHoaDonNoId = null
                };

                _context.ChiTietHoaDonThanhToans.Add(thanhToan);
            }

            entity.LastModified = now;

            await _context.SaveChangesAsync();
            await HoaDonHelper.RecalcConLaiAsync(_context, entity.Id);
            await _context.SaveChangesAsync();
            await AuditDebtGuard.CheckAndNotifyAsync(_context, entity.Id, "CreatePayment");

            var after = ToDto(entity);
            return Result<HoaDonDto>.Success(after, "Đã thu chuyển khoản.")
                .WithId(id).WithBefore(before).WithAfter(after);
        }

        private static bool IsFinalizedByShipper(HoaDon entity)
        {
            var note = entity.GhiChuShipper?.Trim();
            if (string.IsNullOrEmpty(note))
                return false;

            // “Tí nữa chuyển khoản …” chỉ là ghi chú tạm, KHÔNG tính là đã chốt đơn
            if (note.StartsWith("Tí nữa chuyển khoản"))
                return false;

            // Các ghi chú khác coi như đã xử lý xong
            return true;
        }


        public async Task<ShipperSummaryDto> GetSummaryAsync(DateTime ngay, string? shipper = null)
        {
            var start = ngay.Date;
            var end = start.AddDays(1);

            // Các dòng thanh toán trong ngày có GhiChu = 'Shipper'
            var payQ = _context.ChiTietHoaDonThanhToans
                .AsNoTracking()
                .Where(t => !t.IsDeleted
                         && t.Ngay >= start && t.Ngay < end
                         && t.GhiChu == "Shipper");

            // Nếu sau này cần tách theo từng shipper thì có thể join HoaDon.NguoiShip ở đây

            // Trong ngày + trả nợ trong ngày = "Đã thu" (giống ThongKe)
            var daThuQ = payQ.Where(t =>
                t.LoaiThanhToan == "Trong ngày"
                || t.LoaiThanhToan == "Trả nợ trong ngày");

            decimal tienMat = await daThuQ
                .Where(t => t.TenPhuongThucThanhToan == "Tiền mặt")
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal chuyenKhoan = await daThuQ
                .Where(t => t.TenPhuongThucThanhToan != "Tiền mặt")
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            var traNoQ = payQ.Where(t => t.LoaiThanhToan == "Trả nợ qua ngày");

            decimal traNoTrongNgay = await payQ
                .Where(t => t.LoaiThanhToan == "Trả nợ trong ngày")
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            decimal traNoQuaNgay = await traNoQ
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            return new ShipperSummaryDto
            {
                Ngay = start,
                TienMat = tienMat,
                ChuyenKhoan = chuyenKhoan,
                TraNoTrongNgay = traNoTrongNgay,
                TraNoQuaNgay = traNoQuaNgay
            };
        }

    }
}