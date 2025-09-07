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
    private System.Timers.Timer? _fallbackTimer;

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

                    // 🟟 Thông báo rõ ràng hơn cho Hóa đơn
                    if (TuDien._tableFriendlyNames.TryGetValue(entityName, out var friendlyName))
                    {
                        string message = $"{GetActionVerb(action)} {friendlyName.ToLower()}.";

                        if (entityName.Equals("HoaDon", StringComparison.OrdinalIgnoreCase))
                        {
                            var hoaDon = Items.OfType<HoaDonDto>().FirstOrDefault(x => x.Id.ToString() == id);
                            if (hoaDon != null)
                            {
                                message = action switch
                                {
                                    "created" => $"➕ Đơn mới: {hoaDon.MaHoaDon} - {hoaDon.TenKhachHangText ?? hoaDon.TenBan}",
                                    "updated" => $"✏️ Sửa đơn: {hoaDon.MaHoaDon} - {hoaDon.TenKhachHangText ?? hoaDon.TenBan}",
                                    "deleted" => $"🟟️ Xoá đơn: {hoaDon.MaHoaDon} - {hoaDon.TenKhachHangText ?? hoaDon.TenBan}",
                                    "restored" => $"♻️ Khôi phục đơn: {hoaDon.MaHoaDon} - {hoaDon.TenKhachHangText ?? hoaDon.TenBan}",
                                    _ => message
                                };
                            }
                        }

                        // NotiHelper.Show(message);
                    }
                });
            });

            // Khi mất kết nối SignalR → bật fallback timer
            _signalR.OnDisconnected(() =>
            {
                NotiHelper.ShowError("⚠️ Mất kết nối SignalR. Sẽ tự reload mỗi 5 phút...");
                System.Diagnostics.Debug.WriteLine("⚠️ SignalR Disconnected");
                StartFallbackTimer();
            });

            // Khi kết nối lại → tắt fallback timer
            _signalR.OnReconnected(() =>
            {
                NotiHelper.Show("✅ Đã kết nối lại SignalR.");
                System.Diagnostics.Debug.WriteLine("✅ SignalR Reconnected");
                StopFallbackTimer();
            });
        }
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

                System.Diagnostics.Debug.WriteLine($"🟟 Reload {_entityName} thành công ({result.Data.Count} items).");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            System.Diagnostics.Debug.WriteLine($"❌ Reload {_entityName} lỗi: {ex.Message}");
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

    private void StartFallbackTimer()
    {
        if (_fallbackTimer != null) return;

        _fallbackTimer = new System.Timers.Timer(5 * 60 * 1000); // 5 phút
        _fallbackTimer.AutoReset = true;
        _fallbackTimer.Elapsed += async (_, _) =>
        {
            System.Diagnostics.Debug.WriteLine($"⏰ Fallback reload {_entityName}...");
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
}