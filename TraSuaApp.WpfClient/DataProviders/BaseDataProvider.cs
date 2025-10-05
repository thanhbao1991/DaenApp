using System.Collections.ObjectModel;
using System.Net.Http.Json;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Hubs;

namespace TraSuaApp.WpfClient.Providers;

public class BaseDataProvider<T> where T : DtoBase, new()
{
    public ObservableCollection<T> Items { get; private set; } = new();
    public event Action? OnChanged;

    private readonly ISignalRClient? _signalR;
    private readonly string _entityName = (new T()).ApiRoute;

    private System.Timers.Timer? _fallbackTimer;          // khi mất kết nối
    private System.Timers.Timer? _periodicRefreshTimer;   // 🟟 NEW: luôn chạy định kỳ
    private readonly TimeSpan _periodicInterval = TimeSpan.FromMinutes(5); // 🟟 NEW
    private int _isReloading = 0;                         // 🟟 NEW: tránh overlap
    private DateTime _lastReloadAt = DateTime.MinValue;   // 🟟 NEW: theo dõi lần reload gần nhất

    public BaseDataProvider(ISignalRClient? signalR = null)
    {
        _signalR = signalR;
    }

    public async Task InitializeAsync()
    {
        await ReloadAsync();

        if (_signalR != null)
        {
            // Đăng ký nhận realtime update
            _signalR.Subscribe("EntityChanged", async (string entityName, string action, string id, string senderConnectionId) =>
            {
                System.Diagnostics.Debug.WriteLine($"🟟 Nhận signal: {entityName}-{action}-{id}");

                if (!string.Equals(entityName, _entityName, StringComparison.OrdinalIgnoreCase))
                    return;

                if (!string.IsNullOrWhiteSpace(senderConnectionId) &&
                    !string.IsNullOrWhiteSpace(AppProviders.CurrentConnectionId) &&
                    senderConnectionId == AppProviders.CurrentConnectionId)
                    return;

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    if (action == "deleted")
                    {
                        Remove(Guid.Parse(id));
                    }
                    else // created / updated / restored
                    {
                        var item = await LoadByIdAsync(id);
                        if (item != null)
                            OnSignalReceived(item);
                    }

                    // Thông báo riêng cho Hóa đơn (giữ nguyên)
                    if (TuDien._tableFriendlyNames.TryGetValue(entityName, out var friendlyName))
                    {
                        string message = $"{GetActionVerb(action)} {friendlyName.ToLower()}.";

                        if (entityName.Equals("HoaDon", StringComparison.OrdinalIgnoreCase))
                        {
                            var hoaDon = Items.OfType<HoaDonDto>().FirstOrDefault(x => x.Id.ToString() == id);
                            if (hoaDon != null && hoaDon.NguoiShip == "Khánh" && hoaDon.GhiChuShipper != null)
                            {
                                var note = hoaDon.GhiChuShipper.ToLower();
                                if (note.StartsWith("chuyển khoản"))
                                {
                                    AudioHelper.Play("chuyen-khoan.mp3");
                                    NotiHelper.ShowSilent($"{hoaDon.TenKhachHangText} {hoaDon.GhiChuShipper}");
                                }
                                else if (note.StartsWith("ghi nợ"))
                                {
                                    AudioHelper.Play("ghi-no.mp3");
                                    NotiHelper.ShowSilent($"{hoaDon.TenKhachHangText} {hoaDon.GhiChuShipper}");
                                }
                                else if (note.StartsWith("tí nữa chuyển khoản"))
                                {
                                    AudioHelper.Play("chuyen-khoan-sau.mp3");
                                    NotiHelper.ShowSilent($"{hoaDon.TenKhachHangText} {hoaDon.GhiChuShipper}");
                                }
                                else if (note.Contains("trả nợ"))
                                {
                                    AudioHelper.Play("tra-no.mp3");
                                    NotiHelper.ShowSilent($"{hoaDon.TenKhachHangText} {hoaDon.GhiChuShipper}");
                                }
                            }
                        }
                    }
                });
            });

            // Khi mất kết nối SignalR → bật fallback timer (giữ nguyên)
            _signalR.OnDisconnected(() =>
            {
                NotiHelper.ShowError("⚠️ Mất kết nối, Vui lòng chờ...");
                StartFallbackTimer();
            });

            // Khi kết nối lại → tắt fallback timer
            _signalR.OnReconnected(() =>
            {
                NotiHelper.Show("✅ Đã kết nối lại");
                StopFallbackTimer();
            });
        }

        // 🟟 NEW: luôn bật periodic refresh để phòng hụt signal
        StartPeriodicRefreshTimer();
    }

    private string GetActionVerb(string action)
    {
        return action switch
        {
            "created" => "thêm",
            "updated" => "sửa",
            "deleted" => "xoá",
            "restored" => "khôi phục",
            _ => action
        };
    }

    public async Task ReloadAsync()
    {
        // 🟟 NEW: chống overlap reload
        if (Interlocked.Exchange(ref _isReloading, 1) == 1) return;

        try
        {
            var response = await ApiClient.GetAsync($"/api/{_entityName}");
            var result = await response.Content.ReadFromJsonAsync<Result<List<T>>>();
            if (result?.IsSuccess == true && result.Data != null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Items.Clear();
                    foreach (var item in result.Data)
                        Items.Add(item);

                    OnChanged?.Invoke();
                });

                _lastReloadAt = DateTime.UtcNow;
                System.Diagnostics.Debug.WriteLine($"🟟 Reload {_entityName} thành công ({result.Data.Count} items).");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            System.Diagnostics.Debug.WriteLine($"❌ Reload {_entityName} lỗi: {ex.Message}");
        }
        finally
        {
            Interlocked.Exchange(ref _isReloading, 0);
        }
    }

    public void Remove(Guid id)
    {
        var found = Items.FirstOrDefault(x => x.Id == id);
        if (found != null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Items.Remove(found);
                OnChanged?.Invoke();
            });
        }
    }

    private async Task<T?> LoadByIdAsync(string id)
    {
        try
        {
            var response = await ApiClient.GetAsync($"/api/{_entityName}/{id}");
            var result = await response.Content.ReadFromJsonAsync<Result<T>>();
            if (result?.IsSuccess == true && result.Data != null)
                return result.Data;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        return null;
    }

    private void OnSignalReceived(T item)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var existing = Items.FirstOrDefault(x => x.Id == item.Id);
            if (existing != null)
            {
                if (existing is HoaDonDto hoaDon && item is HoaDonDto newHoaDon)
                {
                    hoaDon.CopyFrom(newHoaDon); // giữ nguyên reference
                }
                else
                {
                    var index = Items.IndexOf(existing);
                    Items[index] = item;
                }
            }
            else
            {
                Items.Insert(0, item);
            }

            OnChanged?.Invoke();
        });
    }

    // ========== Fallback khi mất kết nối (giữ nguyên) ==========
    private void StartFallbackTimer()
    {
        if (_fallbackTimer != null) return;

        _fallbackTimer = new System.Timers.Timer(5 * 60 * 1000); // 5 phút
        _fallbackTimer.AutoReset = true;
        _fallbackTimer.Elapsed += async (_, _) =>
        {
            System.Diagnostics.Debug.WriteLine($"⏰ Fallback reload (mất kết nối) - {_entityName}...");
            await ReloadAsync();
        };
        _fallbackTimer.Start();
    }

    private void StopFallbackTimer()
    {
        if (_fallbackTimer != null)
        {
            _fallbackTimer.Stop();
            _fallbackTimer.Dispose();
            _fallbackTimer = null;
        }
    }

    // ========== 🟟 NEW: Periodic refresh luôn chạy ==========
    private void StartPeriodicRefreshTimer()
    {
        if (_periodicRefreshTimer != null) return;

        // Jitter khởi động để tránh tất cả providers cùng nổ một lúc
        var rnd = new Random(Environment.TickCount ^ _entityName.GetHashCode());
        int jitterMs = rnd.Next(10_000, 60_000); // 10–60s

        _periodicRefreshTimer = new System.Timers.Timer(_periodicInterval.TotalMilliseconds);
        _periodicRefreshTimer.AutoReset = true;
        _periodicRefreshTimer.Elapsed += async (_, __) =>
        {
            System.Diagnostics.Debug.WriteLine($"⏰ Periodic reload {_entityName}...");
            await ReloadAsync();
        };

        // Delay khởi động có jitter
        Task.Delay(jitterMs).ContinueWith(_ => _periodicRefreshTimer?.Start());
    }

    // (tuỳ chọn) bạn có thể thêm hàm Dispose() để tắt timer khi cần
}