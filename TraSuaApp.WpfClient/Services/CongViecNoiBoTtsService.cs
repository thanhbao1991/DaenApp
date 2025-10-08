using System.Windows.Threading;

namespace TraSuaApp.WpfClient.Services
{
    /// <summary>
    /// Đọc TTS các công việc nội bộ theo chu kỳ, không phụ thuộc UI/tab.
    /// </summary>
    public sealed class CongViecNoiBoTtsService : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private bool _running;

        // cấu hình
        public bool Enabled { get; set; } = true;
        public int TopN { get; set; } = 5;
        public TimeSpan Interval
        {
            get => _timer.Interval;
            set => _timer.Interval = value;
        }

        public CongViecNoiBoTtsService()
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(5) };
            _timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _timer.Start();

            // Kick 1 vòng đầu khi provider sẵn sàng (fire & forget)
            _ = Task.Run(async () =>
            {
                await WaitForProviderAsync();
                await TickCoreAsync();
            });
        }

        public void Stop()
        {
            _timer.Stop();
            _running = false;
        }

        /// <summary>Chạy ngay một vòng đọc (đợi provider sẵn sàng nếu cần).</summary>
        public async Task KickAsync()
        {
            await WaitForProviderAsync();
            await TickCoreAsync();
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            await TickCoreAsync();
        }

        // Đợi AppProviders.CongViecNoiBos sẵn sàng (tránh NullReference khi app mới khởi động)
        private static async Task<bool> WaitForProviderAsync(int timeoutMs = 8000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (AppProviders.CongViecNoiBos == null && sw.ElapsedMilliseconds < timeoutMs)
                await Task.Delay(100);
            return AppProviders.CongViecNoiBos != null;
        }

        private async Task TickCoreAsync()
        {
            if (!Enabled) return;
            if (!_gate.Wait(0)) return; // chặn overlap

            try
            {
                var provider = AppProviders.CongViecNoiBos;
                if (provider?.Items == null || !provider.Items.Any()) return; // ✅ guard

                // lấy dữ liệu trực tiếp từ provider, tránh phụ thuộc vào UI
                var items = provider.Items
                    .Where(cv => !cv.IsDeleted && !cv.DaHoanThanh)
                    .ToList();

                var today = DateTime.Today;

                var dsHenHomNay = items
                    .Where(cv => cv.NgayCanhBao.HasValue && cv.NgayCanhBao.Value.Date == today)
                    .OrderBy(cv => cv.NgayGio ?? DateTime.MaxValue)
                    .Take(TopN)
                    .ToList();

                var dsChuaHoanThanhTop = items
                    .OrderBy(cv => cv.NgayGio ?? DateTime.MaxValue)
                    .Take(TopN)
                    .ToList();

                var queue = dsHenHomNay.Concat(dsChuaHoanThanhTop)
                                       .GroupBy(x => x.Id)
                                       .Select(g => g.First())
                                       .ToList();

                foreach (var cv in queue)
                {
                    var ten = cv.Ten ?? "";
                    var text = dsHenHomNay.Any(x => x.Id == cv.Id)
                        ? "Kiểm tra " + ten.Replace("Nấu", "")
                        : ten;

                    if (!string.IsNullOrWhiteSpace(text))
                        await TTSHelper.DownloadAndPlayGoogleTTSAsync(text);

                    await Task.Delay(400);
                }
            }
            catch (Exception ex)
            {
                NotiHelper.Show($"TTS lỗi: {ex.Message}");
            }
            finally
            {
                _gate.Release();
            }
        }

        public void Dispose()
        {
            Stop();
            _timer.Tick -= Timer_Tick;   // gỡ handler đúng cách
            _gate.Dispose();
        }
    }
}