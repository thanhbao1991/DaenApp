using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface IVoucherApi
{
    Task<Result<List<VoucherDto>>> GetAllAsync();
    Task<Result<VoucherDto>> GetByIdAsync(Guid id);
    Task<Result<List<VoucherDto>>> GetUpdatedSince(DateTime since);
    Task<Result<VoucherDto>> CreateAsync(VoucherDto dto);
    Task<Result<VoucherDto>> UpdateAsync(Guid id, VoucherDto dto);
    Task<Result<VoucherDto>> DeleteAsync(Guid id);
    Task<Result<VoucherDto>> RestoreAsync(Guid id);
}
