using System.Collections.ObjectModel;
using System.Net.Http.Json;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;
using TraSuaApp.WpfClient.Hubs;

namespace TraSuaApp.WpfClient.Providers;

public class BaseDataProvider<T> where T : DtoBase, new()
{
    public ObservableCollection<T> Items { get; private set; } = new();

    // ❌ cũ (giữ lại nếu cần)
    public event Action? OnChanged;

    // ✅ mới (QUAN TRỌNG)
    public event Action<T>? OnItemChanged;

    private readonly ISignalRClient? _signalR;
    private readonly string _entityName = (new T()).ApiRoute;

    public string EntityName => _entityName;

    private int _isReloading = 0;

    public BaseDataProvider(ISignalRClient? signalR = null)
    {
        _signalR = signalR;
    }

    public async Task InitializeAsync()
    {
        await ReloadAsync();
    }

    // ================================
    // 🟟 SIGNAL CORE
    // ================================
    public async Task HandleSignalAsync(string action, string id)
    {
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            if (action == "deleted")
            {
                Remove(Guid.Parse(id));
                return;
            }

            var item = await LoadByIdAsync(id);
            if (item == null) return;

            Upsert(item);
        });
    }

    // ================================
    // 🟟 UPSERT (có LastModified)
    // ================================
    private void Upsert(T item)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            var existing = Items.FirstOrDefault(x => x.Id == item.Id);

            if (existing != null)
            {
                // 🟟 check LastModified
                if (item.LastModified <= existing.LastModified)
                    return;

                var index = Items.IndexOf(existing);
                Items[index] = item;
            }
            else
            {
                Items.Insert(0, item);
            }

            OnItemChanged?.Invoke(item); // 🟟 dùng cái này cho UI
            OnChanged?.Invoke();         // fallback
        });
    }

    // ================================
    // 🟟 RELOAD (fallback thôi)
    // ================================
    public async Task ReloadAsync()
    {
        if (Interlocked.Exchange(ref _isReloading, 1) == 1) return;

        try
        {
            var url = $"/api/{_entityName}";
            var response = await ApiClient.GetAsync(url);
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

    // ================================
    // 🟟 REMOVE
    // ================================
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

    // ================================
    // 🟟 LOAD BY ID
    // ================================
    private async Task<T?> LoadByIdAsync(string id)
    {
        try
        {
            var response = await ApiClient.GetAsync($"/api/{_entityName}/{id}");
            var result = await response.Content.ReadFromJsonAsync<Result<T>>();
            if (result?.IsSuccess == true && result.Data != null)
                return result.Data;
        }
        catch { }

        return null;
    }
}