using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface IKhachHangGiaBanApi
{
    Task<Result<List<KhachHangGiaBanDto>>> GetAllAsync();
    Task<Result<KhachHangGiaBanDto>> GetByIdAsync(Guid id);
    Task<Result<List<KhachHangGiaBanDto>>> GetUpdatedSince(DateTime since);
    Task<Result<KhachHangGiaBanDto>> CreateAsync(KhachHangGiaBanDto dto);
    Task<Result<KhachHangGiaBanDto>> UpdateAsync(Guid id, KhachHangGiaBanDto dto);
    Task<Result<KhachHangGiaBanDto>> DeleteAsync(Guid id);
    Task<Result<KhachHangGiaBanDto>> RestoreAsync(Guid id);
}
