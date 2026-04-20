//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.SignalR;
//using Microsoft.EntityFrameworkCore;
//using TraSuaApp.Api.Hubs;
//using TraSuaApp.Infrastructure;
//using TraSuaApp.Infrastructure.Entities;
//using TraSuaApp.Shared.Config;
//using TraSuaApp.Infrastructure.Dtos;

//namespace TraSuaApp.Api.Controllers;

//[Authorize]
//[ApiController]
//[Route("api/[controller]")]
//public class ShipperController : BaseApiController
//{
//    private readonly AppDbContext _context;
//    private readonly IHubContext<SignalRHub> _hub;

//    public ShipperController(AppDbContext context, IHubContext<SignalRHub> hub)
//    {
//        _context = context;
//        _hub = hub;
//    }

//    private HoaDonDto ToDto(HoaDon entity)
//    {
//        var pays = entity.ChiTietHoaDonThanhToans?.Where(t => !t.IsDeleted).ToList() ?? new List<ChiTietHoaDonThanhToan>();

//        return new HoaDonDto
//        {
//            Id = entity.Id,
//            MaHoaDon = entity.MaHoaDon,
//            Ngay = entity.Ngay,
//            NgayGio = entity.NgayGio,
//            NgayShip = entity.NgayShip,
//            NguoiShip = entity.NguoiShip,

//            NgayIn = entity.NgayIn,
//            PhanLoai = entity.PhanLoai,
//            TenBan = entity.TenBan,
//            TenKhachHangText = !string.IsNullOrWhiteSpace(entity.TenKhachHangText) ? entity.TenKhachHangText : entity.TenBan,
//            DiaChiText = entity.DiaChiText,
//            SoDienThoaiText = entity.SoDienThoaiText,
//            VoucherId = entity.VoucherId,
//            KhachHangId = entity.KhachHangId,
//            TongTien = entity.TongTien,
//            GiamGia = entity.GiamGia,
//            ThanhTien = entity.ThanhTien,
//            GhiChu = entity.GhiChu,
//            GhiChuShipper = entity.GhiChuShipper,
//            LastModified = entity.LastModified,
//        };
//    }

//    private static string ResolveLoaiThanhToan(HoaDon entity, DateTime now)
//    {
//        if (entity.Ngay.Date == now.Date)
//            return "Trong ngày";

//        return "Trả nợ qua ngày";
//    }

//    private static bool IsFinalizedByShipper(HoaDon entity)
//    {
//        var note = entity.GhiChuShipper?.Trim();
//        if (string.IsNullOrEmpty(note))
//            return false;

//        if (note.StartsWith("Chuyển khoản"))
//            return false;

//        return true;
//    }

//    private async Task NotifyClients(string action, Guid id)
//    {
//        if (!string.IsNullOrEmpty(ConnectionId))
//        {
//            await _hub.Clients
//                .AllExcept(ConnectionId)
//                .SendAsync("EntityChanged", "HoaDon", action, id.ToString(), ConnectionId ?? "");
//        }
//        else
//        {
//            await _hub.Clients.All.SendAsync("EntityChanged", "HoaDon", action, id.ToString(), ConnectionId ?? "");
//        }
//    }

//    [HttpGet("shipper")]
//    [AllowAnonymous]
//    public async Task<ActionResult<Result<List<HoaDonDto>>>> GetForShipper([FromQuery] DateOnly? date, [FromQuery] string? shipper = "Khánh")
//    {
//        DateTime? day = date?.ToDateTime(TimeOnly.MinValue);
//        var start = (day?.Date ?? DateTime.Today);
//        var end = start.AddDays(1);
//        var shipperName = string.IsNullOrWhiteSpace(shipper) ? "Khánh" : shipper.Trim();

//        var list = await _context.HoaDons.AsNoTracking()
//            .Where(x => 
//                && x.PhanLoai == "Ship"
//                && x.Ngay >= start && x.Ngay < end
//                && x.NgayShip != null
//                && x.NguoiShip == shipperName)
//            .OrderByDescending(x => x.NgayGio)
//            .Select(x => new
//            {
//                x.Id,
//                x.TenKhachHangText,
//                x.DiaChiText,
//                x.SoDienThoaiText,
//                x.ThanhTien,
//                x.GhiChu,
//                x.GhiChuShipper,
//                x.NgayGio,
//                x.NgayShip,
//                x.NguoiShip,
//                x.KhachHangId
//            })
//            .ToListAsync();

//        var ids = list.Select(x => x.Id).ToList();
//        var khIds = list.Where(x => x.KhachHangId != null)
//                        .Select(x => x.KhachHangId!.Value)
//                        .Distinct()
//                        .ToList();

//        var conLaiDict = await _context.HoaDonNos
//            .Where(x => ids.Contains(x.Id))
//            .Select(x => new { x.Id, x.ConLai })
//            .ToDictionaryAsync(x => x.Id, x => x.ConLai);

//        var tongNoDict = await _context.HoaDonNos
//            .Where(x => khIds.Contains(x.KhachHangId.Value)
//                     && x.NgayNo != null
//                     && x.ConLai > 0)
//            .GroupBy(x => x.KhachHangId)
//            .Select(g => new
//            {
//                KhachHangId = g.Key,
//                TongNo = g.Sum(x => x.ConLai)
//            })
//            .ToDictionaryAsync(x => x.KhachHangId, x => x.TongNo);

//        var dangGiaoDict = await _context.HoaDonNos
//            .Where(x => khIds.Contains(x.KhachHangId.Value)
//                     && x.NgayNo == null
//                     && x.ConLai > 0)
//            .GroupBy(x => x.KhachHangId)
//            .Select(g => new
//            {
//                KhachHangId = g.Key,
//                TongDangGiao = g.Sum(x => x.ConLai)
//            })
//            .ToDictionaryAsync(x => x.KhachHangId, x => x.TongDangGiao);

//        var result = new List<HoaDonDto>();

//        foreach (var h in list)
//        {
//            var dto = new HoaDonDto
//            {
//                Id = h.Id,
//                TenKhachHangText = h.TenKhachHangText,
//                DiaChiText = h.DiaChiText,
//                SoDienThoaiText = h.SoDienThoaiText,
//                ThanhTien = h.ThanhTien,
//                NgayGio = h.NgayGio,
//                NgayShip = h.NgayShip,
//                NguoiShip = h.NguoiShip,
//                GhiChu = h.GhiChu,
//                GhiChuShipper = h.GhiChuShipper,
//                ConLai = conLaiDict.TryGetValue(h.Id, out var cl) ? cl : 0
//            };

//            if (h.KhachHangId != null)
//            {
//                var khId = h.KhachHangId.Value;

//                dto.TongNoKhachHang =
//                    tongNoDict.TryGetValue(khId, out var tongNo) ? tongNo : 0;

//                var tongDangGiao =
//                    dangGiaoDict.TryGetValue(khId, out var dg) ? dg : 0;

//                dto.TongDonKhacDangGiao =
//                    Math.Max(0, tongDangGiao - dto.ConLai);
//            }

//            result.Add(dto);
//        }

//        return Result<List<HoaDonDto>>.Success(result);
//    }

//    [HttpPost("shipperf1/{id}")]
//    [AllowAnonymous]
//    public async Task<ActionResult<Result<HoaDonDto>>> ThuTienMat(Guid id)
//    {
//        var entity = await _context.HoaDons
//            .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
//            .FirstOrDefaultAsync(x => x.Id == id );

//        if (entity == null)
//            return Result<HoaDonDto>.Failure("Không tìm thấy hoá đơn.");

//        if (entity.ChiTietHoaDonThanhToans.Any(t =>
//                !t.IsDeleted
//                && t.GhiChu == "Shipper"
//                && t.PhuongThucThanhToanId == AppConstants.TienMatId))
//        {
//            return Result<HoaDonDto>.Failure("Shipper đã thu TIỀN MẶT cho đơn này, không thể thu lại.");
//        }

//        if (IsFinalizedByShipper(entity))
//            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");

//        var now = DateTime.Now;
//        var before = ToDto(entity);

//        var daThu = entity.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Sum(t => t.SoTien);
//        var soTienThu = entity.ThanhTien - daThu;

//        if (soTienThu <= 0)
//            return Result<HoaDonDto>.Failure("Hoá đơn đã thu đủ tiền, không thể thu thêm.");

//        if (soTienThu > 0)
//        {
//            var pm = await _context.PhuongThucThanhToans
//                .Where(p =>  p.Id == AppConstants.TienMatId)
//                .Select(p => new { p.Id, p.Ten })
//                .FirstOrDefaultAsync();

//            if (pm == null)
//                return Result<HoaDonDto>.Failure("Không tìm thấy phương thức thanh toán 'Tiền mặt'.");

//            var loai = ResolveLoaiThanhToan(entity, now);

//            var thanhToan = new ChiTietHoaDonThanhToan
//            {
//                Id = Guid.NewGuid(),
//                HoaDonId = entity.Id,
//                KhachHangId = entity.KhachHangId,
//                Ngay = now.Date,
//                NgayGio = now,
//                SoTien = soTienThu,
//                LoaiThanhToan = loai,
//                PhuongThucThanhToanId = pm.Id,
//                GhiChu = "Shipper",
//                LastModified = now,
//                ,
//            };

//            _context.ChiTietHoaDonThanhToans.Add(thanhToan);
//        }

//        entity.GhiChuShipper = $"Tiền mặt";

//        await _context.SaveChangesAsync();

//        var after = ToDto(entity);
//        return Result<HoaDonDto>.Success(after, "Đã thu tiền mặt.")
//            ;
//    }

//    [HttpPost("shipperf4/{id}")]
//    [AllowAnonymous]
//    public async Task<ActionResult<Result<HoaDonDto>>> ThuChuyenKhoan(Guid id)
//    {
//        var entity = await _context.HoaDons
//            .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
//            .FirstOrDefaultAsync(x => x.Id == id );

//        if (entity == null)
//            return Result<HoaDonDto>.Failure("Không tìm thấy hoá đơn.");

//        if (entity.ChiTietHoaDonThanhToans.Any(t =>
//                !t.IsDeleted
//                && t.GhiChu == "Shipper"
//                && t.PhuongThucThanhToanId == AppConstants.TienMatId))
//        {
//            return Result<HoaDonDto>.Failure("Shipper đã thu Chuyển khoản cho đơn này, không thể thu lại.");
//        }

//        if (IsFinalizedByShipper(entity))
//            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");

//        var now = DateTime.Now;
//        var before = ToDto(entity);

//        var daThu = entity.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted).Sum(t => t.SoTien);
//        var soTienThu = entity.ThanhTien - daThu;

//        if (soTienThu <= 0)
//            return Result<HoaDonDto>.Failure("Hoá đơn đã thu đủ tiền, không thể thu thêm.");

//        if (soTienThu > 0)
//        {
//            var pm = await _context.PhuongThucThanhToans
//                .Where(p =>  p.Id == AppConstants.ChuyenKhoanId)
//                .Select(p => new { p.Id, p.Ten })
//                .FirstOrDefaultAsync();

//            if (pm == null)
//                return Result<HoaDonDto>.Failure("Không tìm thấy phương thức thanh toán 'Chuyển khoản'.");

//            var loai = ResolveLoaiThanhToan(entity, now);

//            var thanhToan = new ChiTietHoaDonThanhToan
//            {
//                Id = Guid.NewGuid(),
//                HoaDonId = entity.Id,
//                KhachHangId = entity.KhachHangId,
//                Ngay = now.Date,
//                NgayGio = now,
//                SoTien = soTienThu,
//                LoaiThanhToan = loai,
//                PhuongThucThanhToanId = pm.Id,
//                GhiChu = "Shipper",
//                LastModified = now,
//                ,
//            };

//            _context.ChiTietHoaDonThanhToans.Add(thanhToan);
//        }

//        await _context.SaveChangesAsync();

//        var after = ToDto(entity);
//        return Result<HoaDonDto>.Success(after, "Đã thu Chuyển khoản.")
//            ;
//    }

//    [HttpPost("shipper55/{id}")]
//    [AllowAnonymous]
//    public async Task<ActionResult<Result<HoaDonDto>>> TiNuaChuyenKhoan(Guid id)
//    {
//        var entity = await _context.HoaDons
//            .FirstOrDefaultAsync(x => x.Id == id );

//        if (entity == null)
//            return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

//        if (IsFinalizedByShipper(entity))
//            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");

//        var now = DateTime.Now;
//        var before = ToDto(entity);

//        decimal daThu = await _context.ChiTietHoaDonThanhToans
//            .Where(t => !t.IsDeleted && t.HoaDonId == entity.Id)
//            .SumAsync(t => (decimal?)t.SoTien) ?? 0;

//        decimal conLai = entity.ThanhTien - daThu;
//        entity.GhiChuShipper = $"Tí nữa chuyển khoản";

//        await _context.SaveChangesAsync();

//        var after = ToDto(entity);

//        await DiscordService.SendAsync(
//            DiscordEventType.DuyKhanh,
//            $"{entity.TenKhachHangText} {entity.GhiChuShipper}"
//        );

//        return Result<HoaDonDto>.Success(after, "Cập nhật hóa đơn thành công.")


//            ;
//    }

//    [HttpPost("shipper12/{id}")]
//    [AllowAnonymous]
//    public async Task<ActionResult<Result<HoaDonDto>>> GhiNo(Guid id)
//    {
//        var entity = await _context.HoaDons
//            .FirstOrDefaultAsync(x => x.Id == id);

//        if (entity == null)
//            return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

//        if (IsFinalizedByShipper(entity))
//            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");

//        if (entity.KhachHangId == null)
//            return Result<HoaDonDto>.Failure("Hóa đơn này chưa gắn khách, không thể ghi nợ.");

//        var now = DateTime.Now;
//        var before = ToDto(entity);

//        var conLai = await _context.HoaDonNos
//            .Where(x => x.Id == entity.Id)
//            .Select(x => x.ConLai)
//            .FirstOrDefaultAsync();

//        if (conLai <= 0)
//            return Result<HoaDonDto>.Failure("Hóa đơn đã thanh toán đủ, không thể ghi nợ.");

//        entity.NgayNo = now;
//        entity.GhiChuShipper = "Ghi nợ";

//        await _context.SaveChangesAsync();

//        var after = ToDto(entity);

//        await DiscordService.SendAsync(
//            DiscordEventType.DuyKhanh,
//            $"{entity.TenKhachHangText} {entity.GhiChuShipper}"
//        );

//        return Result<HoaDonDto>.Success(after, "Đã ghi nợ cho hóa đơn.")


//            ;
//    }

//    [HttpPost("shipper99/{id}")]
//    [AllowAnonymous]
//    public async Task<ActionResult<Result<HoaDonDto>>> TraNo(Guid id, [FromBody] decimal soTien)
//    {
//        var entity = await _context.HoaDons
//            .Include(x => x.ChiTietHoaDonThanhToans.Where(t => !t.IsDeleted))
//            .FirstOrDefaultAsync(x => x.Id == id );

//        if (entity == null)
//            return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

//        var now = DateTime.Now;
//        var before = ToDto(entity);

//        if (entity.KhachHangId == null)
//            return Result<HoaDonDto>.Failure("Hoá đơn này không có khách hàng, không thể trả nợ.");

//        decimal soTienThucTe = soTien * 1000;

//        decimal daThuHomNay = entity.ChiTietHoaDonThanhToans.Where(t => t.GhiChu == "Shipper").Sum(t => t.SoTien);
//        decimal soTienTraNo = soTienThucTe - daThuHomNay;
//        if (soTienTraNo <= 0)
//            return Result<HoaDonDto>.Failure("Khách không đưa dư sau phần đã thu của đơn hôm nay.");

//        var khId = entity.KhachHangId.Value;

//        var tongNoCu = await _context.HoaDonNos
//            .Where(x => x.KhachHangId == khId
//                     && x.Id != entity.Id
//                     && x.NgayNo != null
//                     && x.ConLai > 0)
//            .SumAsync(x => (decimal?)x.ConLai) ?? 0;

//        if (tongNoCu <= 0)
//            return Result<HoaDonDto>.Failure("Khách hàng không còn nợ để trả.");

//        decimal soTienCon = Math.Min(soTienTraNo, tongNoCu);
//        decimal traNoCu = 0;

//        var pm = await _context.PhuongThucThanhToans
//            .Where(p =>  p.Id == AppConstants.TienMatId)
//            .Select(p => new { p.Id, p.Ten })
//            .FirstOrDefaultAsync();

//        if (pm == null)
//            return Result<HoaDonDto>.Failure("Không tìm thấy phương thức thanh toán 'Tiền mặt'.");

//        var noConLaiList = await _context.HoaDonNos
//            .Where(n =>
//                      n.KhachHangId == khId
//                     && n.Id != entity.Id
//                     && n.ConLai > 0)
//            .OrderBy(n => n.NgayNo)
//            .ToListAsync();

//        foreach (var n in noConLaiList)
//        {
//            var soNoCon = n.ConLai;
//            if (soNoCon <= 0) continue;

//            var tra = Math.Min(soTienCon, soNoCon);
//            if (tra <= 0) break;

//            _context.ChiTietHoaDonThanhToans.Add(new ChiTietHoaDonThanhToan
//            {
//                Id = Guid.NewGuid(),
//                HoaDonId = n.Id,
//                KhachHangId = khId,
//                Ngay = now.Date,
//                NgayGio = now,
//                SoTien = tra,
//                LoaiThanhToan = n.NgayNo.Value.Date == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày",
//                PhuongThucThanhToanId = pm.Id,
//                GhiChu = "Shipper",
//                LastModified = now,
//                ,
//            });

//            traNoCu += tra;
//            soTienCon -= tra;
//            if (soTienCon <= 0) break;
//        }

//        var ghiChuCu = string.IsNullOrWhiteSpace(entity.GhiChuShipper) ? "" : entity.GhiChuShipper + " | ";
//        entity.GhiChuShipper = $"{ghiChuCu}Trả nợ: {traNoCu:N0} đ";

//        await _context.SaveChangesAsync();
//        var after = ToDto(entity);

//        await DiscordService.SendAsync(DiscordEventType.DuyKhanh, $"{entity.TenKhachHangText} {entity.GhiChuShipper}");

//        return Result<HoaDonDto>.Success(after, "Đã ghi nhận khách trả nợ.")


//            ;
//    }

//    [HttpGet("summary")]
//    [AllowAnonymous]
//    public async Task<ActionResult<ShipperSummaryDto>> Get([FromQuery] DateTime day)
//    {
//        var start = day.Date;
//        var end = start.AddDays(1);

//        var payQ = _context.ChiTietHoaDonThanhToans
//            .AsNoTracking()
//            .Where(t => !t.IsDeleted
//                     && t.Ngay >= start && t.Ngay < end
//                     && t.GhiChu == "Shipper");

//        var daThuQ = payQ.Where(t =>
//            t.LoaiThanhToan == "Trong ngày"
//            || t.LoaiThanhToan == "Trả nợ trong ngày");

//        decimal tienMat = await daThuQ
//            .Where(t => t.PhuongThucThanhToanId == AppConstants.TienMatId)
//            .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

//        decimal chuyenKhoan = await daThuQ
//            .Where(t => t.PhuongThucThanhToanId != AppConstants.TienMatId)
//            .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

//        var traNoQ = payQ.Where(t => t.LoaiThanhToan == "Trả nợ qua ngày");

//        decimal traNoTrongNgay = await payQ
//            .Where(t => t.LoaiThanhToan == "Trả nợ trong ngày")
//            .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

//        decimal traNoQuaNgay = await traNoQ
//            .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

//        var result = new ShipperSummaryDto
//        {
//            Ngay = start,
//            TienMat = tienMat,
//            ChuyenKhoan = chuyenKhoan,
//            TraNoTrongNgay = traNoTrongNgay,
//            TraNoQuaNgay = traNoQuaNgay
//        };

//        return Ok(result);
//    }
//}

using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Entities;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.Shared.Config;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ShipperController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;

    public ShipperController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private HoaDonDto ToDto(HoaDon entity)
    {
        var pays = entity.ChiTietHoaDonThanhToans?.ToList() ?? new List<ChiTietHoaDonThanhToan>();

        return new HoaDonDto
        {
            Id = entity.Id,
            MaHoaDon = entity.MaHoaDon,
            Ngay = entity.Ngay,
            NgayGio = entity.NgayGio,
            NgayShip = entity.NgayShip,
            NguoiShip = entity.NguoiShip,
            NgayIn = entity.NgayIn,
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
            LastModified = entity.LastModified,
        };
    }

    private static string ResolveLoaiThanhToan(HoaDon entity, DateTime now)
    {
        if (entity.Ngay.Date == now.Date)
            return "Trong ngày";

        return "Trả nợ qua ngày";
    }

    private static bool IsFinalizedByShipper(HoaDon entity)
    {
        var note = entity.GhiChuShipper?.Trim();
        if (string.IsNullOrEmpty(note))
            return false;

        if (note.StartsWith("Chuyển khoản"))
            return false;

        return true;
    }

    private async Task<bool> DaThuTheoPhuongThucAsync(Guid hoaDonId, Guid phuongThucId)
    {
        return await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .AnyAsync(t =>

                t.HoaDonId == hoaDonId &&
                t.GhiChu == "Shipper" &&
                t.PhuongThucThanhToanId == phuongThucId);
    }

    private async Task NotifyClients(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients
                .AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "HoaDon", action, id.ToString(), ConnectionId ?? "");
        }
        else
        {
            await _hub.Clients.All.SendAsync("EntityChanged", "HoaDon", action, id.ToString(), ConnectionId ?? "");
        }
    }

    [HttpGet("shipper")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<List<HoaDonDto>>>> GetForShipper([FromQuery] DateOnly? date, [FromQuery] string? shipper = "Khánh")
    {
        DateTime? day = date?.ToDateTime(TimeOnly.MinValue);
        var start = (day?.Date ?? DateTime.Today);
        var end = start.AddDays(1);
        var shipperName = string.IsNullOrWhiteSpace(shipper) ? "Khánh" : shipper.Trim();

        var list = await _context.HoaDons.AsNoTracking()
            .Where(x =>
                x.PhanLoai == "Ship"
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
                x.NgayGio,
                x.NgayShip,
                x.NguoiShip,
                x.KhachHangId
            })
            .ToListAsync();

        var ids = list.Select(x => x.Id).ToList();
        var khIds = list.Where(x => x.KhachHangId != null)
                        .Select(x => x.KhachHangId!.Value)
                        .Distinct()
                        .ToList();

        var conLaiDict = await _context.HoaDonNos
            .Where(x => ids.Contains(x.Id))
            .Select(x => new { x.Id, x.ConLai })
            .ToDictionaryAsync(x => x.Id, x => x.ConLai);

        var tongNoDict = await _context.HoaDonNos
            .Where(x => khIds.Contains(x.KhachHangId.Value)
                     && x.NgayNo != null
                     && x.ConLai > 0)
            .GroupBy(x => x.KhachHangId)
            .Select(g => new
            {
                KhachHangId = g.Key,
                TongNo = g.Sum(x => x.ConLai)
            })
            .ToDictionaryAsync(x => x.KhachHangId, x => x.TongNo);

        var dangGiaoDict = await _context.HoaDonNos
            .Where(x => khIds.Contains(x.KhachHangId.Value)
                     && x.NgayNo == null
                     && x.ConLai > 0)
            .GroupBy(x => x.KhachHangId)
            .Select(g => new
            {
                KhachHangId = g.Key,
                TongDangGiao = g.Sum(x => x.ConLai)
            })
            .ToDictionaryAsync(x => x.KhachHangId, x => x.TongDangGiao);

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
                NgayGio = h.NgayGio,
                NgayShip = h.NgayShip,
                NguoiShip = h.NguoiShip,
                GhiChu = h.GhiChu,
                GhiChuShipper = h.GhiChuShipper,
                ConLai = conLaiDict.TryGetValue(h.Id, out var cl) ? cl : 0
            };

            if (h.KhachHangId != null)
            {
                var khId = h.KhachHangId.Value;

                dto.TongNoKhachHang =
                    tongNoDict.TryGetValue(khId, out var tongNo) ? tongNo : 0;

                var tongDangGiao =
                    dangGiaoDict.TryGetValue(khId, out var dg) ? dg : 0;

                dto.TongDonKhacDangGiao =
                    Math.Max(0, tongDangGiao - dto.ConLai);
            }

            result.Add(dto);
        }

        return Result<List<HoaDonDto>>.Success(result);
    }

    [HttpPost("shipperf1/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<HoaDonDto>>> ThuTienMat(Guid id)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var entity = await _context.HoaDons
            .Include(x => x.ChiTietHoaDonThanhToans)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hoá đơn.");

        if (IsFinalizedByShipper(entity))
            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");

        if (await DaThuTheoPhuongThucAsync(id, AppConstants.TienMatId))
            return Result<HoaDonDto>.Failure("Shipper đã thu TIỀN MẶT cho đơn này, không thể thu lại.");

        var now = DateTime.Now;

        var daThu = entity.ChiTietHoaDonThanhToans.Sum(t => t.SoTien);
        var soTienThu = entity.ThanhTien - daThu;

        if (soTienThu <= 0)
            return Result<HoaDonDto>.Failure("Hoá đơn đã thu đủ tiền, không thể thu thêm.");

        var pm = await _context.PhuongThucThanhToans
            .Where(p => p.Id == AppConstants.TienMatId)
            .Select(p => new { p.Id, p.Ten })
            .FirstOrDefaultAsync();

        if (pm == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy phương thức thanh toán 'Tiền mặt'.");

        var loai = ResolveLoaiThanhToan(entity, now);

        _context.ChiTietHoaDonThanhToans.Add(new ChiTietHoaDonThanhToan
        {
            Id = Guid.NewGuid(),
            HoaDonId = entity.Id,
            KhachHangId = entity.KhachHangId,
            Ngay = now.Date,
            NgayGio = now,
            SoTien = soTienThu,
            LoaiThanhToan = loai,
            PhuongThucThanhToanId = pm.Id,
            GhiChu = "Shipper",
            LastModified = now
        });

        entity.GhiChuShipper = "Tiền mặt";

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return Result<HoaDonDto>.Success(ToDto(entity), "Đã thu tiền mặt.");
    }

    [HttpPost("shipperf4/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<HoaDonDto>>> ThuChuyenKhoan(Guid id)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var entity = await _context.HoaDons
            .Include(x => x.ChiTietHoaDonThanhToans)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hoá đơn.");

        if (IsFinalizedByShipper(entity))
            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");

        if (await DaThuTheoPhuongThucAsync(id, AppConstants.ChuyenKhoanId))
            return Result<HoaDonDto>.Failure("Shipper đã thu Chuyển khoản cho đơn này, không thể thu lại.");

        var now = DateTime.Now;

        var daThu = entity.ChiTietHoaDonThanhToans.Sum(t => t.SoTien);
        var soTienThu = entity.ThanhTien - daThu;

        if (soTienThu <= 0)
            return Result<HoaDonDto>.Failure("Hoá đơn đã thu đủ tiền, không thể thu thêm.");

        var pm = await _context.PhuongThucThanhToans
            .Where(p => p.Id == AppConstants.ChuyenKhoanId)
            .Select(p => new { p.Id, p.Ten })
            .FirstOrDefaultAsync();

        if (pm == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy phương thức thanh toán 'Chuyển khoản'.");

        var loai = ResolveLoaiThanhToan(entity, now);

        _context.ChiTietHoaDonThanhToans.Add(new ChiTietHoaDonThanhToan
        {
            Id = Guid.NewGuid(),
            HoaDonId = entity.Id,
            KhachHangId = entity.KhachHangId,
            Ngay = now.Date,
            NgayGio = now,
            SoTien = soTienThu,
            LoaiThanhToan = loai,
            PhuongThucThanhToanId = pm.Id,
            GhiChu = "Shipper",
            LastModified = now,
        });

        entity.GhiChuShipper = "Chuyển khoản";

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        return Result<HoaDonDto>.Success(ToDto(entity), "Đã thu Chuyển khoản.");
    }

    [HttpPost("shipper55/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<HoaDonDto>>> TiNuaChuyenKhoan(Guid id)
    {
        var entity = await _context.HoaDons
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

        if (IsFinalizedByShipper(entity))
            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");

        var now = DateTime.Now;

        decimal daThu = await _context.ChiTietHoaDonThanhToans
            .Where(t => t.HoaDonId == entity.Id)
            .SumAsync(t => (decimal?)t.SoTien) ?? 0;

        decimal conLai = entity.ThanhTien - daThu;
        entity.GhiChuShipper = $"Tí nữa chuyển khoản";

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await DiscordService.SendAsync(
            DiscordEventType.DuyKhanh,
            $"{entity.TenKhachHangText} {entity.GhiChuShipper}"
        );

        return Result<HoaDonDto>.Success(after, "Cập nhật hóa đơn thành công.");
    }

    [HttpPost("shipper12/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<HoaDonDto>>> GhiNo(Guid id)
    {
        var entity = await _context.HoaDons
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

        if (IsFinalizedByShipper(entity))
            return Result<HoaDonDto>.Failure("Đơn này đã được shipper xử lý, không thể thao tác lại.");

        if (entity.KhachHangId == null)
            return Result<HoaDonDto>.Failure("Hóa đơn này chưa gắn khách, không thể ghi nợ.");

        var now = DateTime.Now;

        var conLai = await _context.HoaDonNos
            .Where(x => x.Id == entity.Id)
            .Select(x => x.ConLai)
            .FirstOrDefaultAsync();

        if (conLai <= 0)
            return Result<HoaDonDto>.Failure("Hóa đơn đã thanh toán đủ, không thể ghi nợ.");

        entity.NgayNo = now;
        entity.GhiChuShipper = "Ghi nợ";

        await _context.SaveChangesAsync();

        var after = ToDto(entity);

        await DiscordService.SendAsync(
            DiscordEventType.DuyKhanh,
            $"{entity.TenKhachHangText} {entity.GhiChuShipper}"
        );

        return Result<HoaDonDto>.Success(after, "Đã ghi nợ cho hóa đơn.");
    }

    [HttpPost("shipper99/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Result<HoaDonDto>>> TraNo(Guid id, [FromBody] decimal soTien)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var entity = await _context.HoaDons
            .Include(x => x.ChiTietHoaDonThanhToans)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy hóa đơn.");

        if (entity.KhachHangId == null)
            return Result<HoaDonDto>.Failure("Hoá đơn này không có khách hàng, không thể trả nợ.");

        var now = DateTime.Now;

        decimal soTienThucTe = soTien * 1000;

        decimal daThuHomNay = entity.ChiTietHoaDonThanhToans.Where(t => t.GhiChu == "Shipper").Sum(t => t.SoTien);
        decimal soTienTraNo = soTienThucTe - daThuHomNay;

        if (soTienTraNo <= 0)
            return Result<HoaDonDto>.Failure("Khách không đưa dư sau phần đã thu của đơn hôm nay.");

        var khId = entity.KhachHangId.Value;

        var tongNoCu = await _context.HoaDonNos
            .Where(x => x.KhachHangId == khId
                     && x.Id != entity.Id
                     && x.NgayNo != null
                     && x.ConLai > 0)
            .SumAsync(x => (decimal?)x.ConLai) ?? 0;

        if (tongNoCu <= 0)
            return Result<HoaDonDto>.Failure("Khách hàng không còn nợ để trả.");

        decimal soTienCon = Math.Min(soTienTraNo, tongNoCu);
        decimal traNoCu = 0;

        var pm = await _context.PhuongThucThanhToans
            .Where(p => p.Id == AppConstants.TienMatId)
            .Select(p => new { p.Id, p.Ten })
            .FirstOrDefaultAsync();

        if (pm == null)
            return Result<HoaDonDto>.Failure("Không tìm thấy phương thức thanh toán 'Tiền mặt'.");

        var noConLaiList = await _context.HoaDonNos
            .Where(n =>
                      n.KhachHangId == khId
                     && n.Id != entity.Id
                     && n.ConLai > 0)
            .OrderBy(n => n.NgayNo)
            .ToListAsync();

        foreach (var n in noConLaiList)
        {
            var soNoCon = n.ConLai;
            if (soNoCon <= 0) continue;

            var tra = Math.Min(soTienCon, soNoCon);
            if (tra <= 0) break;

            _context.ChiTietHoaDonThanhToans.Add(new ChiTietHoaDonThanhToan
            {
                Id = Guid.NewGuid(),
                HoaDonId = n.Id,
                KhachHangId = khId,
                Ngay = now.Date,
                NgayGio = now,
                SoTien = tra,
                LoaiThanhToan = n.NgayNo.Value.Date == now.Date ? "Trả nợ trong ngày" : "Trả nợ qua ngày",
                PhuongThucThanhToanId = pm.Id,
                GhiChu = "Shipper",
                LastModified = now,
            });

            traNoCu += tra;
            soTienCon -= tra;
            if (soTienCon <= 0) break;
        }

        var ghiChuCu = string.IsNullOrWhiteSpace(entity.GhiChuShipper) ? "" : entity.GhiChuShipper + " | ";
        entity.GhiChuShipper = $"{ghiChuCu}Trả nợ: {traNoCu:N0} đ";

        await _context.SaveChangesAsync();
        await tx.CommitAsync();

        var after = ToDto(entity);

        await DiscordService.SendAsync(
            DiscordEventType.DuyKhanh,
            $"{entity.TenKhachHangText} {entity.GhiChuShipper}"
        );

        return Result<HoaDonDto>.Success(after, "Đã ghi nhận khách trả nợ.");
    }

    [HttpGet("summary")]
    [AllowAnonymous]
    public async Task<ActionResult<ShipperSummaryDto>> Get([FromQuery] DateTime day)
    {
        var start = day.Date;
        var end = start.AddDays(1);

        var payQ = _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(t => t.Ngay >= start && t.Ngay < end
                     && t.GhiChu == "Shipper");

        var daThuQ = payQ.Where(t =>
            t.LoaiThanhToan == "Trong ngày"
            || t.LoaiThanhToan == "Trả nợ trong ngày");

        decimal tienMat = await daThuQ
            .Where(t => t.PhuongThucThanhToanId == AppConstants.TienMatId)
            .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

        decimal chuyenKhoan = await daThuQ
            .Where(t => t.PhuongThucThanhToanId != AppConstants.TienMatId)
            .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

        var traNoQ = payQ.Where(t => t.LoaiThanhToan == "Trả nợ qua ngày");

        decimal traNoTrongNgay = await payQ
            .Where(t => t.LoaiThanhToan == "Trả nợ trong ngày")
            .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

        decimal traNoQuaNgay = await traNoQ
            .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

        var result = new ShipperSummaryDto
        {
            Ngay = start,
            TienMat = tienMat,
            ChuyenKhoan = chuyenKhoan,
            TraNoTrongNgay = traNoTrongNgay,
            TraNoQuaNgay = traNoQuaNgay
        };

        return Ok(result);
    }
}