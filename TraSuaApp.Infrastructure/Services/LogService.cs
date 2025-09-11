using Microsoft.EntityFrameworkCore;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.Shared.Mappings;

namespace TraSuaApp.Infrastructure.Services;

public class LogService : ILogService
{
    private readonly AppDbContext _context;

    public LogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<LogDto>> GetAllAsync()
    {
        var list = await _context.Logs.AsNoTracking()
            .OrderByDescending(x => x.ThoiGian).ToListAsync();
        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<List<LogDto>> GetByDateAsync(DateTime date)
    {
        var list = await _context.Logs.AsNoTracking()
            .Where(x => x.ThoiGian.Date == date.Date)
            .OrderByDescending(x => x.ThoiGian)
            .ToListAsync();

        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<List<LogDto>> GetByEntityIdAsync(Guid entityId)
    {
        var list = await _context.Logs.AsNoTracking()
            .Where(x => x.EntityId == entityId)
            .OrderByDescending(x => x.ThoiGian)
            .ToListAsync();

        return list.Select(x => x.ToDto()).ToList();
    }

    public async Task<LogDto?> GetByIdAsync(Guid id)
    {
        var log = await _context.Logs.FindAsync(id);
        return log?.ToDto();
    }

    public async Task<Result<LogDto>> CreateAsync(LogDto dto)
    {
        var entity = dto.ToEntity();
        entity.Id = Guid.NewGuid();
        entity.ThoiGian = DateTime.Now;

        await _context.Logs.AddAsync(entity);
        await _context.SaveChangesAsync();

        return Result<LogDto>.Success("Đã ghi log", entity.ToDto()).WithId(entity.Id);
    }

    public async Task<Result<LogDto>> UpdateAsync(Guid id, LogDto dto)
    {
        var log = await _context.Logs.FindAsync(id);
        if (log == null)
            return Result<LogDto>.Failure("Không tìm thấy log");

        dto.UpdateEntity(log);
        await _context.SaveChangesAsync();

        return Result<LogDto>.Success("Đã cập nhật log", log.ToDto()).WithId(id);
    }

    public async Task<Result<LogDto>> DeleteAsync(Guid id)
    {
        var log = await _context.Logs.FindAsync(id);
        if (log == null)
            return Result<LogDto>.Failure("Không tìm thấy log");

        _context.Logs.Remove(log);
        await _context.SaveChangesAsync();

        return Result<LogDto>.Success("Đã xoá log", log.ToDto()).WithId(id);
    }
}
