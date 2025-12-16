using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces;

public interface ISuDungNguyenLieuService
{
    Task<List<SuDungNguyenLieuDto>> GetAllAsync();
    Task<SuDungNguyenLieuDto?> GetByIdAsync(Guid id);
    Task<Result<SuDungNguyenLieuDto>> CreateAsync(SuDungNguyenLieuDto dto);
    Task<Result<SuDungNguyenLieuDto>> UpdateAsync(Guid id, SuDungNguyenLieuDto dto);
    Task<Result<SuDungNguyenLieuDto>> DeleteAsync(Guid id);
    Task<Result<SuDungNguyenLieuDto>> RestoreAsync(Guid id);
    Task<List<SuDungNguyenLieuDto>> GetUpdatedSince(DateTime lastSync);
}
