using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;

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

    public async Task LogAsync(Log log)
    {
        _context.Logs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResultDto<LogDto>> GetLogsAsync(LogFilterDto filter)
    {
        var query = _context.Logs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.UserName))
            query = query.Where(x => x.UserName!.Contains(filter.UserName));

        if (!string.IsNullOrWhiteSpace(filter.Path))
            query = query.Where(x => x.Path!.Contains(filter.Path));

        if (filter.StatusCode.HasValue)
            query = query.Where(x => x.StatusCode == filter.StatusCode);

        if (filter.TuNgay.HasValue)
            query = query.Where(x => x.ThoiGian >= filter.TuNgay);

        if (filter.DenNgay.HasValue)
            query = query.Where(x => x.ThoiGian <= filter.DenNgay);

        var total = await query.CountAsync();

        var logs = await query
            .OrderByDescending(x => x.ThoiGian)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = _mapper.Map<List<LogDto>>(logs);

        return new PagedResultDto<LogDto>(items, total);
    }

    public async Task<Log?> GetLogByIdAsync(Guid id)
    {
        return await _context.Logs.FindAsync(id);
    }
}