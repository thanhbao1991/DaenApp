using System.Collections.ObjectModel;
using System.Net.Http.Json;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.Shared.Enums;
using TraSuaApp.Shared.Helpers;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Providers;

public class BaseDataProvider<T> where T : DtoBase, new()
{
    public ObservableCollection<T> Items { get; private set; } = new();
    public event Action? OnChanged;

    private readonly ISignalRClient? _signalR;
    private readonly string _entityName = (new T()).ApiRoute;
    private System.Timers.Timer? _timer;

    public BaseDataProvider(ISignalRClient? signalR = null)
    {
        _signalR = signalR;
    }

    public async Task InitializeAsync()
    {
        await ReloadAsync();

        if (_signalR != null)
        {
            await _signalR.ConnectAsync();

            _signalR.Subscribe("EntityChanged", async (string entityName, string action, string id, string senderConnectionId) =>
            {
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

                    if (TuDien._tableFriendlyNames.TryGetValue(entityName, out var friendlyName))
                    {
                        ToastHelper.Show($"Cập nhật {GetActionVerb(action)} {friendlyName.ToLower()}.");
                    }
                });
            });
        }

        _timer = new System.Timers.Timer(60000);
        _timer.Elapsed += async (_, _) => await ReloadAsync();
        _timer.Start();
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
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
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
                var index = Items.IndexOf(existing);
                Items[index] = item;
            }
            else
            {
                Items.Insert(0, item);
            }

            OnChanged?.Invoke();
        });
    }
}