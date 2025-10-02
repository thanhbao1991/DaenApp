using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface IKhachHangGiaBanService
{
    Task<List<KhachHangGiaBanDto>> GetAllAsync();
    Task<KhachHangGiaBanDto?> GetByIdAsync(Guid id);
    Task<Result<KhachHangGiaBanDto>> CreateAsync(KhachHangGiaBanDto dto);
    Task<Result<KhachHangGiaBanDto>> UpdateAsync(Guid id, KhachHangGiaBanDto dto);
    Task<Result<KhachHangGiaBanDto>> DeleteAsync(Guid id);
    Task<Result<KhachHangGiaBanDto>> RestoreAsync(Guid id);
    Task<List<KhachHangGiaBanDto>> GetUpdatedSince(DateTime lastSync);
}
