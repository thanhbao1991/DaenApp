using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.WpfClient.Apis;

public interface ISuDungNguyenLieuApi
{
    Task<Result<List<SuDungNguyenLieuDto>>> GetAllAsync();
    Task<Result<SuDungNguyenLieuDto>> GetByIdAsync(Guid id);
    Task<Result<List<SuDungNguyenLieuDto>>> GetUpdatedSince(DateTime since);
    Task<Result<SuDungNguyenLieuDto>> CreateAsync(SuDungNguyenLieuDto dto);
    Task<Result<SuDungNguyenLieuDto>> UpdateAsync(Guid id, SuDungNguyenLieuDto dto);
    Task<Result<SuDungNguyenLieuDto>> DeleteAsync(Guid id);
    Task<Result<SuDungNguyenLieuDto>> RestoreAsync(Guid id);
}
