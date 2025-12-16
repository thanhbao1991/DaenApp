using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface INguyenLieuTransactionApi
{
    Task<Result<List<NguyenLieuTransactionDto>>> GetAllAsync();
    Task<Result<NguyenLieuTransactionDto>> GetByIdAsync(Guid id);
    Task<Result<List<NguyenLieuTransactionDto>>> GetUpdatedSince(DateTime since);
    Task<Result<NguyenLieuTransactionDto>> CreateAsync(NguyenLieuTransactionDto dto);
    Task<Result<NguyenLieuTransactionDto>> UpdateAsync(Guid id, NguyenLieuTransactionDto dto);
    Task<Result<NguyenLieuTransactionDto>> DeleteAsync(Guid id);
    Task<Result<NguyenLieuTransactionDto>> RestoreAsync(Guid id);
}
