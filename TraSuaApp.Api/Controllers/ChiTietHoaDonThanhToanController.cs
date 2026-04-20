using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Api.Hubs;
using TraSuaApp.Infrastructure;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Entities;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ChiTietHoaDonThanhToanController : BaseApiController
{
    private readonly AppDbContext _context;
    private readonly IHubContext<SignalRHub> _hub;

    public ChiTietHoaDonThanhToanController(AppDbContext context, IHubContext<SignalRHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    private static ChiTietHoaDonThanhToanDto ToDto(ChiTietHoaDonThanhToan e)
    {
        return new ChiTietHoaDonThanhToanDto
        {
            Id = e.Id,
            SoTien = e.SoTien,
            Ngay = e.Ngay,
            NgayGio = e.NgayGio,
            HoaDonId = e.HoaDonId,
            KhachHangId = e.KhachHangId,
            PhuongThucThanhToanId = e.PhuongThucThanhToanId,
            GhiChu = e.GhiChu,
            LastModified = e.LastModified
        };
    }

    private async Task Notify(string action, Guid id)
    {
        if (!string.IsNullOrEmpty(ConnectionId))
        {
            await _hub.Clients.AllExcept(ConnectionId)
                .SendAsync("EntityChanged", "ChiTietHoaDonThanhToan", action, id.ToString(), ConnectionId);
        }
        else
        {
            await _hub.Clients.All
                .SendAsync("EntityChanged", "ChiTietHoaDonThanhToan", action, id.ToString(), "");
        }
    }

    // ======================
    // GET ALL
    // ======================
    [HttpGet]
    public async Task<ActionResult<Result<List<ChiTietHoaDonThanhToanDto>>>> GetAll(DateTime? ngay)
    {
        var targetDate = ngay?.Date ?? DateTime.Today;
        var nextDate = targetDate.AddDays(1);

        var list = await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .Where(x =>
                        x.NgayGio >= targetDate &&
                        x.NgayGio < nextDate)
            .OrderByDescending(x => x.NgayGio)
            .Select(x => new ChiTietHoaDonThanhToanDto
            {
                Id = x.Id,
                SoTien = x.SoTien,
                Ngay = x.Ngay,
                NgayGio = x.NgayGio,
                HoaDonId = x.HoaDonId,
                KhachHangId = x.KhachHangId,
                PhuongThucThanhToanId = x.PhuongThucThanhToanId,
                GhiChu = x.GhiChu,
                LastModified = x.LastModified
            })
            .ToListAsync();

        return Result<List<ChiTietHoaDonThanhToanDto>>.Success(list);
    }

    // ======================
    // GET BY ID
    // ======================
    [HttpGet("{id}")]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> GetById(Guid id)
    {
        var entity = await _context.ChiTietHoaDonThanhToans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Không tìm thấy.");

        return Result<ChiTietHoaDonThanhToanDto>.Success(ToDto(entity));
    }

    // ======================
    // CREATE
    // ======================
    [HttpPost]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> Create(ChiTietHoaDonThanhToanDto dto)
    {
        if (dto.SoTien < 0)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Số tiền không hợp lệ.");

        var hoaDon = await _context.HoaDons
            .FirstOrDefaultAsync(x => x.Id == dto.HoaDonId);

        if (hoaDon == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Hóa đơn không tồn tại.");

        var tongDaThanhToan = await _context.ChiTietHoaDonThanhToans
            .Where(x => x.HoaDonId == dto.HoaDonId)
            .SumAsync(x => x.SoTien);

        var conLai = hoaDon.ThanhTien - tongDaThanhToan;

        if (dto.SoTien > conLai)
            return Result<ChiTietHoaDonThanhToanDto>.Failure($"Còn lại: {conLai:N0}");

        var now = DateTime.Now;

        var entity = new ChiTietHoaDonThanhToan
        {
            Id = Guid.NewGuid(),
            SoTien = dto.SoTien,
            HoaDonId = dto.HoaDonId,
            KhachHangId = dto.KhachHangId,
            PhuongThucThanhToanId = dto.PhuongThucThanhToanId,
            GhiChu = dto.SoTien == conLai ? "Thanh toán đủ" : "Thanh toán thiếu",
            Ngay = now.Date,
            NgayGio = now,
            LastModified = now
        };

        _context.ChiTietHoaDonThanhToans.Add(entity);
        await _context.SaveChangesAsync();

        await Notify("created", entity.Id);

        return Result<ChiTietHoaDonThanhToanDto>.Success(ToDto(entity), "Đã thêm")
            ;
    }

    // ======================
    // DELETE (HARD DELETE)
    // ======================
    [HttpDelete("{id}")]
    public async Task<ActionResult<Result<ChiTietHoaDonThanhToanDto>>> Delete(Guid id)
    {
        var entity = await _context.ChiTietHoaDonThanhToans
            .FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null)
            return Result<ChiTietHoaDonThanhToanDto>.Failure("Không tìm thấy.");

        _context.ChiTietHoaDonThanhToans.Remove(entity);
        await _context.SaveChangesAsync();

        await Notify("deleted", id);

        return Result<ChiTietHoaDonThanhToanDto>.Success(null, "Đã xoá")
            ;
    }

    // ======================
    // DELETE BY HOADON
    // ======================
    [HttpDelete("byHoaDon/{hoaDonId}")]
    public async Task<ActionResult<Result<bool>>> DeleteByHoaDon(Guid hoaDonId)
    {
        var list = await _context.ChiTietHoaDonThanhToans
            .Where(x => x.HoaDonId == hoaDonId)
            .ToListAsync();

        _context.ChiTietHoaDonThanhToans.RemoveRange(list);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}