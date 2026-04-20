using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TraSuaApp.Shared.Config;
using TraSuaApp.WpfClient.DataProviders;
using TraSuaApp.WpfClient.Helpers;

namespace TraSuaApp.WpfClient.Views
{
    public partial class Dashboard : Window, INotifyPropertyChanged
    {
        public static bool IsThanhToanHidden = true;
        public static HashSet<Guid> VisibleHoaDonIds = new();
        public static HashSet<Guid> HoaDonDaCoThanhToanIds = new();

        // ====== Constants / Keys (tránh magic string) ======
        private const string TAB_TAG_HOADON = "HoaDon";
        private const string TAB_TAG_THONGKE = "ThongKe";
        private const string TAB_TAG_CTHD_NO = "ChiTietHoaDonNo";
        private const string TAB_TAG_CTHD_TT = "ChiTietHoaDonThanhToan";
        private const string TAB_TAG_CHITIEU = "ChiTieuHangNgay";

        // ====== INotifyPropertyChanged ======
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private DateTime _today;
        private DateTime Today
        {
            get => _today;
            set { _today = value; OnPropertyChanged(nameof(Today)); }
        }

        public Dashboard()
        {
            InitializeComponent();

            foreach (TabItem tab in TabControl.Items)
                if (tab.ToolTip?.ToString() == "-")
                    tab.Visibility = Visibility.Collapsed;

            DataContext = this;
            Loaded += Dashboard_Loaded;

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;

            GenerateMenu("Admin", AdminMenu);
            GenerateMenu("Settings", SettingsMenu);
        }

        private void UpdateCongViecBadge()
        {
            var items = AppProviders.CongViecNoiBos?.Items;
            if (items == null) return;

            int count = items.Count(x => !x.DaHoanThanh);
            Dispatcher.Invoke(() =>
            {
                CvBadgeText.Text = count.ToString();
                CvBadge.Visibility = count > 0 ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private async void Dashboard_Loaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                Today = DateTime.Today;

                await AppProviders.ReloadAllAsync();
                UpdateCongViecBadge();

                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(15));
                    try
                    {
                        string shippingUsername = "12122431577";
                        string shippingPassword = "baothanh1991";

                        await AppShippingHelperFactory.CreateAsync(shippingUsername, shippingPassword);
                    }
                    catch
                    {
                    }
                });
            }
            catch (Exception ex)
            {
                NotiHelper.Show("Lỗi tải Dashboard: " + ex.Message);
            }
        }

        // ====== Busy indicator helper ======
        private async Task WithBusy(Func<Task> body)
        {
            if (ProgressBar != null)
                ProgressBar.Visibility = Visibility.Visible;

            try { await body(); }
            finally
            {
                if (ProgressBar != null)
                    ProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        // ====== Menu động (Admin/Hóa đơn/Settings) ======
        private void GenerateMenu(string loai, MenuItem m)
        {
            var viewType = typeof(Window);
            var views = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(viewType)
                            && (t.FullName?.Contains(loai) ?? false)
                            && t.Name.Contains("List"))
                .OrderBy(t => t.Name);

            foreach (var view in views.OrderBy(x => x.Name))
            {
                string friendly = view.Name.Replace("List", "").Replace("Edit", "");
                string header = (TuDien._tableFriendlyNames != null &&
                                 TuDien._tableFriendlyNames.TryGetValue(friendly, out var lbl) &&
                                 !string.IsNullOrWhiteSpace(lbl))
                                ? lbl
                                : friendly;

                var btn = new MenuItem
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Header = header,
                    Tag = view.Name,
                    ToolTip = loai,
                    Margin = new Thickness(4)
                };
                btn.Click += MenuButton_Click;

                if (view.Name == "SuDungNguyenLieuList" ||
                    view.Name == "NguyenLieuTransactionList" ||
                    view.Name == "LocationList")
                {
                    continue;
                }

                m.Items.Add(btn);
            }
        }

        private async void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem btn || btn.Tag is not string tag) return;

            try
            {
                var namespaceName = $"TraSuaApp.WpfClient.{btn.ToolTip}Views";
                var typeName = $"{namespaceName}.{tag}";
                var type = Type.GetType(typeName);

                if (type == null)
                {
                    NotiHelper.ShowError($"Không tìm thấy form: {tag}");
                    return;
                }

                if (Activator.CreateInstance(type) is Window window)
                {
                    window.Width = Width;
                    window.Height = Height;
                    window.Owner = this;
                    window.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                var real = ex.InnerException?.ToString() ?? ex.ToString();
                NotiHelper.ShowError(real);
                Clipboard.SetText(real);
            }
        }

        // ====== Title bar buttons ======
        private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
                MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        // ====== Import Noti Window ======
        void Import_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var existing = Application.Current.Windows
                    .OfType<TraSuaApp.WpfClient.Views.NotiWindow>()
                    .FirstOrDefault();

                if (existing == null)
                {
                    var win = new TraSuaApp.WpfClient.Views.NotiWindow();
                    win.Show();
                }
                else
                {
                    existing.Topmost = true;
                    existing.Focus();
                }
            });
        }

        // ====== Chuyển Tab ======
        private async void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (TabItem tab in TabControl.Items)
                if (tab.ToolTip?.ToString() == "-")
                    tab.Visibility = Visibility.Collapsed;

            IsThanhToanHidden = true;

            if (!ReferenceEquals(e.OriginalSource, sender)) return;
            if (sender is not System.Windows.Controls.TabControl) return;

            FrameworkElement? oldContent = (e.RemovedItems.Count > 0 && e.RemovedItems[0] is TabItem oldTab)
                ? oldTab.Content as FrameworkElement
                : null;

            FrameworkElement? newContent = TabControl.SelectedContent as FrameworkElement;
            await AnimationHelper.FadeSwitchAsync(oldContent, newContent);

            if (TabControl.SelectedItem is not TabItem selectedTab) return;
            string? tag = selectedTab.Tag?.ToString();
            if (string.IsNullOrEmpty(tag)) return;

            if (tag == TAB_TAG_THONGKE && selectedTab.Content is ThongKeTab thongKeTab)
            {
                thongKeTab.ReloadToday();
            }

            var loadActions = new Dictionary<string, Func<Task>>
            {
                [TAB_TAG_HOADON] = async () =>
                {
                    HoaDonTabControl?.RefreshVisibleItemsOnly();
                    await Task.CompletedTask;
                },

                [TAB_TAG_CHITIEU] = async () =>
                {
                    if (ChiTieuHangNgayTabControl != null)
                    {
                        ChiTieuHangNgayTabControl.Today = Today;
                        ChiTieuHangNgayTabControl.ReloadUI();
                    }
                    await Task.CompletedTask;
                },

                [TAB_TAG_CTHD_TT] = async () =>
                {
                    if (ChiTietHoaDonThanhToanTabControl != null)
                    {
                        ChiTietHoaDonThanhToanTabControl.Today = Today;
                        ChiTietHoaDonThanhToanTabControl.ReloadUI();
                    }
                    await Task.CompletedTask;
                },
            };

            if (loadActions.TryGetValue(tag, out var action))
                await action();

            if (tag == TAB_TAG_CHITIEU && ChiTieuHangNgayTabControl != null)
            {
                ChiTieuHangNgayTabControl.TriggerAddNew();
            }
        }

        // ====== Hotkeys: forward xuống Tab con ======
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F9)
            {
                TraSuaApp.WpfClient.Tools.FileViewerWindowList fileViewerWindow = new TraSuaApp.WpfClient.Tools.FileViewerWindowList();
                fileViewerWindow.ShowDialog();
                e.Handled = true;
                return;
            }
            if (e.Key == Key.F5)
            {
                _ = ForceReloadCurrentTabAsync();
                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Down)
            {
                foreach (TabItem t in TabControl.Items)
                    if (t.ToolTip?.ToString() == "-")
                        t.Visibility = Visibility.Visible;

                IsThanhToanHidden = false;
                _ = HoaDonTabControl.ReloadAndRestoreSelectionAsync();
                e.Handled = true;
                return;
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Up)
            {
                foreach (TabItem t in TabControl.Items)
                    if (t.ToolTip?.ToString() == "-")
                        t.Visibility = Visibility.Collapsed;

                IsThanhToanHidden = true;
                _ = HoaDonTabControl.ReloadAndRestoreSelectionAsync();
                return;
            }

            if (Keyboard.FocusedElement is TextBox)
            {
                bool isHotkey =
                    (e.Key == Key.F1 || e.Key == Key.F4) ||
                    (e.Key >= Key.F1 && e.Key <= Key.F24) ||
                    e.Key == Key.Escape ||
                    e.Key == Key.Delete;

                if (!isHotkey) return;
            }

            if (TabControl.SelectedItem is not TabItem tab) return;
            var tag = tab.Tag?.ToString();

            if (tag == TAB_TAG_HOADON && HoaDonTabControl != null)
            {
                HoaDonTabControl.HandleHotkey(e.Key);

                if ((e.Key >= Key.F1 && e.Key <= Key.F24) ||
                    e.Key == Key.Escape ||
                    e.Key == Key.Delete ||
                    e.Key == Key.Enter ||
                    e.Key == Key.Space)
                {
                    e.Handled = true;
                }
            }
            else if (tag == TAB_TAG_CTHD_NO && ChiTietHoaDonNoTabControl != null)
            {
                ChiTietHoaDonNoTabControl.HandleHotkey(e.Key);

                if (e.Key == Key.F1 || e.Key == Key.F4)
                    e.Handled = true;
            }
        }

        private async Task ForceReloadCurrentTabAsync()
        {
            if (TabControl.SelectedItem is not TabItem tab) return;

            string? tag = tab.Tag?.ToString();

            switch (tag)
            {
                case TAB_TAG_HOADON:
                    await WithBusy(async () =>
                    {
                        await HoaDonTabControl.ReloadAndRestoreSelectionAsync();
                    });
                    break;

                case TAB_TAG_CTHD_TT:
                    await WithBusy(async () =>
                    {
                        await AppProviders.ChiTietHoaDonThanhToans.ReloadAsync();
                        ChiTietHoaDonThanhToanTabControl.ReloadUI();
                    });
                    break;

                case TAB_TAG_CHITIEU:
                    await WithBusy(async () =>
                    {
                        await AppProviders.ChiTieuHangNgays.ReloadAsync();
                        ChiTieuHangNgayTabControl.ReloadUI();
                    });
                    break;
            }
        }


    }
}