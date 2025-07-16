using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface IKhachHangApi
{
    Task<Result<List<KhachHangDto>>> GetAllAsync();
    Task<Result<KhachHangDto>> GetByIdAsync(Guid id);
    Task<Result<KhachHangDto>> CreateAsync(KhachHangDto dto);
    Task<Result<KhachHangDto>> UpdateAsync(Guid id, KhachHangDto dto);
    Task<Result<KhachHangDto>> DeleteAsync(Guid id);
    Task<Result<KhachHangDto>> RestoreAsync(Guid id);
    Task<Result<List<KhachHangDto>>> GetUpdatedSince(DateTime since);
}