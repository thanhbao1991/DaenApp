using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Infrastructure.Services;

public class LogService : ILogService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public LogService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<LogDto>> GetAllAsync()
    {
        var list = await _context.Logs.OrderByDescending(x => x.ThoiGian).ToListAsync();
        return _mapper.Map<List<LogDto>>(list);
    }

    public async Task<List<LogDto>> GetByDateAsync(DateTime date)
    {
        var list = await _context.Logs
            .Where(x => x.ThoiGian.Date == date.Date)
            .OrderByDescending(x => x.ThoiGian)
            .ToListAsync();

        return _mapper.Map<List<LogDto>>(list);
    }

    public async Task<List<LogDto>> GetByEntityIdAsync(Guid entityId)
    {
        var list = await _context.Logs
            .Where(x => x.EntityId == entityId)
            .OrderByDescending(x => x.ThoiGian)
            .ToListAsync();

        return _mapper.Map<List<LogDto>>(list);
    }

    public async Task<LogDto?> GetByIdAsync(Guid id)
    {
        var log = await _context.Logs.FindAsync(id);
        return _mapper.Map<LogDto>(log);
    }

    public async Task<Result<LogDto>> CreateAsync(LogDto dto)
    {
        var entity = _mapper.Map<Log>(dto);
        entity.Id = Guid.NewGuid();
        entity.ThoiGian = DateTime.Now;

        await _context.Logs.AddAsync(entity);
        await _context.SaveChangesAsync();

        var createdDto = _mapper.Map<LogDto>(entity);
        return Result<LogDto>.Success("Đã ghi log", createdDto).WithId(entity.Id);
    }

    public async Task<Result<LogDto>> UpdateAsync(Guid id, LogDto dto)
    {
        var log = await _context.Logs.FindAsync(id);
        if (log == null)
            return Result<LogDto>.Failure("Không tìm thấy log");

        _mapper.Map(dto, log);
        await _context.SaveChangesAsync();

        var updatedDto = _mapper.Map<LogDto>(log);
        return Result<LogDto>.Success("Đã cập nhật log", updatedDto).WithId(id);
    }

    public async Task<Result<LogDto>> DeleteAsync(Guid id)
    {
        var log = await _context.Logs.FindAsync(id);
        if (log == null)
            return Result<LogDto>.Failure("Không tìm thấy log");

        _context.Logs.Remove(log);
        await _context.SaveChangesAsync();

        return Result<LogDto>.Success("Đã xoá log", _mapper.Map<LogDto>(log)).WithId(id);
    }
}