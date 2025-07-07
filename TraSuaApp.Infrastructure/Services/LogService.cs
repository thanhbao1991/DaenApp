using Microsoft.EntityFrameworkCore;
using TraSuaApp.Application.Interfaces;
using TraSuaApp.Domain.Entities;
using TraSuaApp.Infrastructure.Data;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Infrastructure.Services
{
    public class LogService : ILogService
    {
        private readonly AppDbContext _context;

        public LogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(Log log)
        {
            _context.Logs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResult<LogDto>> GetLogsAsync(LogFilterDto filter)
        {
            var query = _context.Logs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.UserName))
                query = query.Where(x => x.UserName != null && x.UserName.Contains(filter.UserName));

            if (!string.IsNullOrWhiteSpace(filter.Path))
                query = query.Where(x => x.Path != null && x.Path.Contains(filter.Path));

            if (filter.StatusCode.HasValue)
                query = query.Where(x => x.StatusCode == filter.StatusCode.Value);

            if (filter.TuNgay.HasValue)
                query = query.Where(x => x.ThoiGian >= filter.TuNgay.Value);

            if (filter.DenNgay.HasValue)
                query = query.Where(x => x.ThoiGian <= filter.DenNgay.Value);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(x => x.ThoiGian)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new LogDto
                {
                    Id = x.Id,
                    ThoiGian = x.ThoiGian,
                    UserName = x.UserName,
                    Method = x.Method,
                    Path = x.Path,
                    StatusCode = x.StatusCode,
                    DurationMs = x.DurationMs
                })
                .ToListAsync();

            return new PagedResult<LogDto>(items, total);
        }

        public async Task<Log?> GetLogByIdAsync(Guid id)
        {
            return await _context.Logs.FindAsync(id);
        }
    }
}