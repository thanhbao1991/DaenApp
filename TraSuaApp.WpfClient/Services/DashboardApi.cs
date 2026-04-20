using TraSuaApp.Shared.Config;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;

namespace TraSuaApp.WpfClient.Services
{
    public class DashboardApi : BaseApi<object>
    {
        private const string BASE_URL = "/api/dashboard";

        public DashboardApi()
            : base(BASE_URL, TuDien._tableFriendlyNames["Dashboard"])
        {
        }

        // =========================
        // GET
        // =========================

        public Task<Result<List<HoaDonNoDto>>> GetHoaDon(CancellationToken ct = default)
            => GetAsync<List<HoaDonNoDto>>($"{BASE_URL}/get-hoa-don", ct);

        public Task<Result<List<HoaDonNoDto>>> GetCongNo(CancellationToken ct = default)
            => GetAsync<List<HoaDonNoDto>>($"{BASE_URL}/get-cong-no", ct);

    }
}