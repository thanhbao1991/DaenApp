using TraSuaApp.Domain.Entities;
using TraSuaApp.Shared.Dtos;

namespace TraSuaApp.Application.Interfaces
{
    public interface ILogService
    {
        Task LogAsync(Log log); // Ghi log
        Task<PagedResultDto<LogDto>> GetLogsAsync(LogFilterDto filter); // Truy vấn danh sách
        Task<Log?> GetLogByIdAsync(Guid id); // Xem chi tiết
    }
}