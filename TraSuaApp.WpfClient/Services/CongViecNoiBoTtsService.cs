using System.Windows.Threading;

namespace TraSuaApp.WpfClient.Services
{
    /// Đọc TTS các công việc nội bộ theo mốc 10 phút: 00/10/20/30/40/50
    public sealed class CongViecNoiBoTtsService : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private readonly SemaphoreSlim _gate = new(1, 1);
        private bool _running;

        public bool Enabled { get; set; } = true;
        public int TopN { get; set; } = 5;

        // Giữ property cho tương thích, nhưng ta luôn chạy theo mốc 10'
        public TimeSpan Interval { get => TimeSpan.FromMinutes(10); set { /* ignore */ } }

        public CongViecNoiBoTtsService()
        {
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            ScheduleNextTick();           // canh đúng mốc tiếp theo
        }

        public void Stop()
        {
            _timer.Stop();
            _running = false;
        }

        // (tuỳ chọn) chỉ dùng test thủ công
        public async Task KickAsync() => await TickCoreAsync();

        private void ScheduleNextTick()
        {
            var now = DateTime.Now;
            // phút mốc (bội số của 10) kế tiếp, giây = 0
            int nextMinuteBlock = ((now.Minute / 10) * 10) + 10;
            var next = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
            next = next.AddMinutes(nextMinuteBlock);
            if (next.Minute == 60) next = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(1);

            // nếu vô tình đúng mốc rồi, đẩy nhẹ 1s để tránh double
            var due = next - now;
            if (due <= TimeSpan.Zero) due = TimeSpan.FromSeconds(1);

            _timer.Stop();
            _timer.Interval = due;
            _timer.Start();
        }

        private async void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();                // one-shot
            try
            {
                if (Enabled) await TickCoreAsync();
            }
            finally
            {
                ScheduleNextTick();       // luôn canh lại mốc 10' kế tiếp
            }
        }

        private async Task TickCoreAsync()
        {
            if (!Enabled) return;
            if (!_gate.Wait(0)) return;

            try
            {
                var provider = AppProviders.CongViecNoiBos;
                if (provider?.Items == null || !provider.Items.Any()) return;

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
                                       .GroupBy(x => x.Id)          // bỏ trùng theo Id
                                       .Select(g => g.First())
                                       .ToList();

                // Tránh trùng text trong cùng 1 tick
                var sentTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var cv in queue)
                {
                    var ten = cv.Ten ?? "";
                    var text = dsHenHomNay.Any(x => x.Id == cv.Id)
                        ? "Kiểm tra " + ten.Replace("Nấu", "")
                        : ten;

                    if (string.IsNullOrWhiteSpace(text)) continue;
                    if (!sentTexts.Add(text)) continue;

                    await TTSHelper.DownloadAndPlayGoogleTTSAsync(text);
                    //DiscordService.SendAsync(Shared.Enums.DiscordEventType.Admin, text);

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
            _timer.Tick -= Timer_Tick;
            _gate.Dispose();
        }
    }
}