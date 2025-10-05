// Applicationn/Interfaces/IShipperService.cs
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces
{
    public interface IShipperService
    {
        Task<List<HoaDonDto>> GetForShipperAsync();
        Task<Result<HoaDonDto>> ThuTienMatAsync(Guid id);
        Task<Result<HoaDonDto>> ThuChuyenKhoanAsync(Guid id);
        Task<Result<HoaDonDto>> GhiNoAsync(Guid id);
        Task<Result<HoaDonDto>> TiNuaChuyenKhoanAsync(Guid id);
        Task<Result<HoaDonDto>> TraNoAsync(Guid id, decimal soTienKhachDua);
    }
}