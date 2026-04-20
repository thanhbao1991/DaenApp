using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Data;
using Microsoft.AspNetCore.SignalR.Client;
using TraSuaApp.Infrastructure.Dtos;
using TraSuaApp.Infrastructure.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.DataProviders
{
    // ============================
    // BASE
    // ============================
    public class BaseDataProvider<T> where T : DtoBase, new()
    {
        public ObservableCollection<T> Items { get; private set; } = new();

        public event Action? OnChanged;
        public event Action<T>? OnItemChanged;

        private readonly string _entityName = (new T()).ApiRoute;
        private int _isReloading = 0;

        public string EntityName => _entityName;

        public async Task InitializeAsync()
        {
            await ReloadAsync();
        }

        public async Task ReloadAsync()
        {
            if (Interlocked.Exchange(ref _isReloading, 1) == 1) return;

            try
            {
                var response = await ApiClient.GetAsync($"/api/{_entityName}");
                var result = await response.Content.ReadFromJsonAsync<Result<List<T>>>();

                if (result?.IsSuccess == true && result.Data != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Items.Clear();
                        foreach (var item in result.Data)
                            Items.Add(item);

                        OnChanged?.Invoke();
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _isReloading, 0);
            }
        }
        public void Remove(Guid id)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var found = Items.FirstOrDefault(x => x.Id == id);
                if (found != null)
                {
                    Items.Remove(found);

                    CollectionViewSource.GetDefaultView(Items)?.Refresh(); // 🟟 THÊM

                    OnChanged?.Invoke();
                }
            });
        }
        public void Upsert(T item)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var existing = Items.FirstOrDefault(x => x.Id == item.Id);

                if (existing != null)
                {
                    if (item.LastModified <= existing.LastModified)
                        return;

                    var index = Items.IndexOf(existing);
                    Items[index] = item;
                }
                else
                {
                    Items.Insert(0, item);
                }

                CollectionViewSource.GetDefaultView(Items)?.Refresh(); // 🟟 THÊM

                OnItemChanged?.Invoke(item);
                OnChanged?.Invoke();
            });
        }
        public virtual async Task<T?> LoadByIdAsync(string id)
        {
            try
            {
                var response = await ApiClient.GetAsync($"/api/{_entityName}/{id}");
                var result = await response.Content.ReadFromJsonAsync<Result<T>>();

                if (result?.IsSuccess == true)
                    return result.Data;
            }
            catch
            {
            }

            return null;
        }
    }

    public class TuDienTraCuuDataProvider : BaseDataProvider<TuDienTraCuuDto> { }
    public class NguyenLieuBanHangDataProvider : BaseDataProvider<NguyenLieuBanHangDto> { }
    public class CongThucDataProvider : BaseDataProvider<CongThucDto> { }
    public class SuDungNguyenLieuDataProvider : BaseDataProvider<SuDungNguyenLieuDto> { }
    public class VoucherDataProvider : BaseDataProvider<VoucherDto> { }
    public class SanPhamDataProvider : BaseDataProvider<SanPhamDto> { }
    public class SanPhamBienTheDataProvider : BaseDataProvider<SanPhamBienTheDto> { }
    public class TaiKhoanDataProvider : BaseDataProvider<TaiKhoanDto> { }
    public class ToppingDataProvider : BaseDataProvider<ToppingDto> { }
    public class KhachHangDataProvider : BaseDataProvider<KhachHangDto> { }
    public class NhomSanPhamDataProvider : BaseDataProvider<NhomSanPhamDto> { }
    public class HoaDonDataProvider : BaseDataProvider<HoaDonDto> { }

    // THÊM MỚI
    public class HoaDonNoDataProvider : BaseDataProvider<HoaDonNoDto>
    {
        public override async Task<HoaDonNoDto?> LoadByIdAsync(string id)
        {
            try
            {
                var response = await ApiClient.GetAsync($"/api/Dashboard/get-hoa-don/{id}");
                var result = await response.Content.ReadFromJsonAsync<Result<HoaDonNoDto>>();

                if (result?.IsSuccess == true)
                    return result.Data;
            }
            catch
            {
            }

            return null;
        }
    }
    public class PhuongThucThanhToanDataProvider : BaseDataProvider<PhuongThucThanhToanDto> { }
    public class KhachHangGiaBanDataProvider : BaseDataProvider<KhachHangGiaBanDto> { }

    public class CongViecNoiBoDataProvider : BaseDataProvider<CongViecNoiBoDto>
    {
        public event EventHandler? ItemsChanged;

        public CongViecNoiBoDataProvider()
        {
            OnChanged += () => ItemsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class ChiTietHoaDonThanhToanDataProvider : BaseDataProvider<ChiTietHoaDonThanhToanDto> { }
    public class ChiTieuHangNgayDataProvider : BaseDataProvider<ChiTieuHangNgayDto> { }
    public class NguyenLieuDataProvider : BaseDataProvider<NguyenLieuDto> { }


    public static class AppProviders
    {
        public static TuDienTraCuuDataProvider? TuDienTraCuus { get; private set; }
        public static NguyenLieuBanHangDataProvider? NguyenLieuBanHangs { get; private set; }
        public static CongThucDataProvider? CongThucs { get; private set; }
        public static SuDungNguyenLieuDataProvider? SuDungNguyenLieus { get; private set; }
        public static VoucherDataProvider? Vouchers { get; private set; }
        public static SanPhamDataProvider? SanPhams { get; private set; }
        public static TaiKhoanDataProvider? TaiKhoans { get; private set; }
        public static ToppingDataProvider? Toppings { get; private set; }
        public static KhachHangDataProvider? KhachHangs { get; private set; }
        public static NhomSanPhamDataProvider? NhomSanPhams { get; private set; }
        public static HoaDonDataProvider? HoaDons { get; private set; }

        // THÊM MỚI
        public static HoaDonNoDataProvider? HoaDonNos { get; private set; }

        public static PhuongThucThanhToanDataProvider? PhuongThucThanhToans { get; private set; }
        public static KhachHangGiaBanDataProvider? KhachHangGiaBans { get; private set; }
        public static CongViecNoiBoDataProvider? CongViecNoiBos { get; private set; }
        public static ChiTietHoaDonThanhToanDataProvider? ChiTietHoaDonThanhToans { get; private set; }
        public static ChiTieuHangNgayDataProvider? ChiTieuHangNgays { get; private set; }
        public static NguyenLieuDataProvider? NguyenLieus { get; private set; }

        public static TraSuaApp.WpfClient.Hubs.SignalRClient? SignalR { get; set; }

        public static string QuickOrderMenu { get; private set; } = "";

        private static bool _created;
        private static readonly ConcurrentDictionary<string, byte> _processing = new();

        public static void BuildQuickOrderMenu()
        {
            var items = SanPhams?.Items?
                .Where(x => !x.NgungBan)
                .OrderBy(x => x.Ten)
                .Select(x => $"{x.Id}\t{x.TenKhongVietTat}")
                ?? Enumerable.Empty<string>();

            QuickOrderMenu = string.Join("\n", items);
        }

        private static async Task RefreshHoaDonProvidersAsync(string id)
        {
            if (HoaDonNos != null)
            {
                var itemNo = await HoaDonNos.LoadByIdAsync(id);
                if (itemNo != null)
                    HoaDonNos.Upsert(itemNo);
            }
        }

        private static void RemoveHoaDonProviders(Guid id)
        {
            HoaDonNos?.Remove(id);
        }

        public static async Task ReloadAllAsync()
        {
            if (HoaDons != null) await HoaDons.ReloadAsync();
            if (HoaDonNos != null) await HoaDonNos.ReloadAsync();
            if (ChiTietHoaDonThanhToans != null) await ChiTietHoaDonThanhToans.ReloadAsync();
            if (ChiTieuHangNgays != null) await ChiTieuHangNgays.ReloadAsync();
            if (CongViecNoiBos != null) await CongViecNoiBos.ReloadAsync();
            if (Toppings != null) await Toppings.ReloadAsync();
            if (KhachHangs != null) await KhachHangs.ReloadAsync();
            if (Vouchers != null) await Vouchers.ReloadAsync();
            if (KhachHangGiaBans != null) await KhachHangGiaBans.ReloadAsync();
            if (PhuongThucThanhToans != null) await PhuongThucThanhToans.ReloadAsync();
            if (TuDienTraCuus != null) await TuDienTraCuus.ReloadAsync();
            if (NguyenLieuBanHangs != null) await NguyenLieuBanHangs.ReloadAsync();
            if (CongThucs != null) await CongThucs.ReloadAsync();
            if (SuDungNguyenLieus != null) await SuDungNguyenLieus.ReloadAsync();

            if (SanPhams != null)
            {
                await SanPhams.ReloadAsync();
                BuildQuickOrderMenu();
            }
        }

        public static async Task EnsureCreatedAsync()
        {
            if (_created) return;

            TuDienTraCuus = new();
            NguyenLieuBanHangs = new();
            CongThucs = new();
            SuDungNguyenLieus = new();
            Vouchers = new();
            SanPhams = new();
            TaiKhoans = new();
            Toppings = new();
            KhachHangs = new();
            NhomSanPhams = new();
            HoaDons = new();

            // THÊM MỚI
            HoaDonNos = new();

            PhuongThucThanhToans = new();
            KhachHangGiaBans = new();
            CongViecNoiBos = new();
            ChiTietHoaDonThanhToans = new();
            ChiTieuHangNgays = new();
            NguyenLieus = new();

            SanPhams.OnChanged += BuildQuickOrderMenu;

            _created = true;
            await Task.CompletedTask;
        }

        public static async Task InitializeAsync()
        {
            await EnsureCreatedAsync();

            await Task.WhenAll(
                KhachHangs!.InitializeAsync(),
                NhomSanPhams!.InitializeAsync(),
                Toppings!.InitializeAsync(),
                TaiKhoans!.InitializeAsync(),
                SanPhams!.InitializeAsync(),
                Vouchers!.InitializeAsync(),
                HoaDons!.InitializeAsync(),
                HoaDonNos!.InitializeAsync(),
                PhuongThucThanhToans!.InitializeAsync(),
                TuDienTraCuus!.InitializeAsync(),
                NguyenLieuBanHangs!.InitializeAsync(),
                CongThucs!.InitializeAsync(),
                SuDungNguyenLieus!.InitializeAsync(),
                KhachHangGiaBans!.InitializeAsync(),
                CongViecNoiBos!.InitializeAsync(),
                ChiTietHoaDonThanhToans!.InitializeAsync(),
                NguyenLieus!.InitializeAsync(),
                ChiTieuHangNgays!.InitializeAsync()
            );

            BuildQuickOrderMenu();
        }

        public static async Task HandleSignalAsync(string action, string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return;

            var key = $"{action}:{id}";
            if (!_processing.TryAdd(key, 0))
                return;

            try
            {
                switch (action)
                {
                    case "CREATE":
                    case "UPDATE":
                    case "F12":
                    case "ESC":
                    case "PRINT":
                        await RefreshHoaDonProvidersAsync(id);
                        break;

                    case "DEL":
                        RemoveHoaDonProviders(guid);
                        break;

                    case "F1":
                    case "F4":
                    case "ROLLBACK":
                        await RefreshHoaDonProvidersAsync(id);
                        if (ChiTietHoaDonThanhToans != null)
                            await ChiTietHoaDonThanhToans.ReloadAsync();
                        break;

                    default:
                        Console.WriteLine($"Unknown signal: {action}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Signal handle error ({action}): {ex.Message}");
            }
            finally
            {
                _processing.TryRemove(key, out _);
            }
        }
    }
}

namespace TraSuaApp.WpfClient.Hubs
{
    using TraSuaApp.WpfClient.DataProviders;

    public interface ISignalRClient
    {
        Task ConnectAsync();
        Task<string?> GetConnectionId();
        void OnDisconnected(Action onDisconnected);
        void OnReconnected(Action onReconnected);
    }

    public class SignalRClient : ISignalRClient
    {
        private readonly HubConnection _connection;
        private Action? _onDisconnected;
        private Action? _onReconnected;
        private string? _connectionId;

        public SignalRClient(string hubUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            RegisterSignals();

            _connection.Closed += async _ =>
            {
                Console.WriteLine("SignalR disconnected");
                _onDisconnected?.Invoke();
                await Task.CompletedTask;
            };

            _connection.Reconnecting += async _ =>
            {
                Console.WriteLine("SignalR reconnecting...");
                await Task.CompletedTask;
            };

            _connection.Reconnected += async _ =>
            {
                Console.WriteLine("SignalR reconnected");

                await RefreshConnectionIdAsync();
                await AppProviders.ReloadAllAsync();

                _onReconnected?.Invoke();
            };
        }

        private void RegisterSignals()
        {
            Register("CREATE");
            Register("UPDATE");
            Register("DEL");
            Register("F1");
            Register("F4");
            Register("F12");
            Register("ESC");
            Register("ROLLBACK");
            Register("PRINT");
        }

        private void Register(string signal)
        {
            _connection.On<SignalMessageDto>(signal, async msg =>
            {
                if (msg == null)
                    return;

                if (!string.IsNullOrWhiteSpace(_connectionId) &&
                    string.Equals(msg.SenderConnectionId, _connectionId, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                Console.WriteLine($"Signal: {signal} - {msg.Id}");
                await ForwardAsync(signal, msg.Id);
            });
        }

        private async Task ForwardAsync(string action, string id)
        {
            try
            {
                await AppProviders.HandleSignalAsync(action, id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Signal error ({action}): {ex.Message}");
            }
        }

        private async Task RefreshConnectionIdAsync()
        {
            try
            {
                _connectionId = await _connection.InvokeAsync<string>("GetConnectionId");
                ApiClient.ConnectionId = _connectionId;
            }
            catch
            {
                _connectionId = null;
                ApiClient.ConnectionId = null;
            }
        }

        public async Task ConnectAsync()
        {
            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync();
                Console.WriteLine("SignalR connected");
            }

            await RefreshConnectionIdAsync();
        }

        public void OnDisconnected(Action onDisconnected) => _onDisconnected = onDisconnected;
        public void OnReconnected(Action onReconnected) => _onReconnected = onReconnected;

        public async Task<string?> GetConnectionId()
        {
            if (!string.IsNullOrWhiteSpace(_connectionId))
                return _connectionId;

            try
            {
                _connectionId = await _connection.InvokeAsync<string>("GetConnectionId");
                return _connectionId;
            }
            catch
            {
                return null;
            }
        }
    }
}