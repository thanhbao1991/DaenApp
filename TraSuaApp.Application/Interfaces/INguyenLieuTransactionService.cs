using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface INguyenLieuTransactionService
{
    Task<List<NguyenLieuTransactionDto>> GetAllAsync();
    Task<NguyenLieuTransactionDto?> GetByIdAsync(Guid id);
    Task<Result<NguyenLieuTransactionDto>> CreateAsync(NguyenLieuTransactionDto dto);
    Task<Result<NguyenLieuTransactionDto>> UpdateAsync(Guid id, NguyenLieuTransactionDto dto);
    Task<Result<NguyenLieuTransactionDto>> DeleteAsync(Guid id);
    Task<Result<NguyenLieuTransactionDto>> RestoreAsync(Guid id);
    Task<List<NguyenLieuTransactionDto>> GetUpdatedSince(DateTime lastSync);
}
