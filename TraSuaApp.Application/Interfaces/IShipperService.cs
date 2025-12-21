using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;

namespace TraSuaApp.Applicationn.Interfaces
{
    public interface IShipperService
    {
        Task<ShipperSummaryDto> GetSummaryAsync(DateTime ngay, string? shipper = null);
        Task<List<HoaDonDto>> GetForShipperAsync(DateTime? day = null, string? shipper = "Khánh");
        Task<Result<HoaDonDto>> ThuTienMatAsync(Guid id);
        Task<Result<HoaDonDto>> ThuChuyenKhoanAsync(Guid id);
        Task<Result<HoaDonDto>> GhiNoAsync(Guid id);
        Task<Result<HoaDonDto>> TiNuaChuyenKhoanAsync(Guid id);
        Task<Result<HoaDonDto>> TraNoAsync(Guid id, decimal soTienKhachDua);
    }
}