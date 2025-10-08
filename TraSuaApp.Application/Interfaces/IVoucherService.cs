using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface IVoucherService
{
    Task<List<VoucherDto>> GetAllAsync();
    Task<VoucherDto?> GetByIdAsync(Guid id);
    Task<Result<VoucherDto>> CreateAsync(VoucherDto dto);
    Task<Result<VoucherDto>> UpdateAsync(Guid id, VoucherDto dto);
    Task<Result<VoucherDto>> DeleteAsync(Guid id);
    Task<Result<VoucherDto>> RestoreAsync(Guid id);
    Task<List<VoucherDto>> GetUpdatedSince(DateTime lastSync);


}
