using System.IO;
using System.Windows;
using System.Windows.Input;
using TraSuaApp.Shared.Dtos;
using TraSuaApp.WpfClient.Controls;


namespace TraSuaApp.WpfClient.HoaDonViews
{
    public partial class HoaDonEdit
    {
        private void SetGptBusy(bool on, string? text = null)
        {
            try
            {
                GptBusyOverlay.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
                if (text != null) GptBusyText.Text = text;
                Mouse.OverrideCursor = on ? Cursors.AppStarting : null;
            }
            catch { }
        }

        private async Task RunGptFromMessengerIfNeededAsync(string latestCustomerName, string input)
        {
            try
            {
                if (!_openedFromMessenger) return;
                if (string.IsNullOrWhiteSpace(input)) return;

                bool isImage = File.Exists(input);

                Guid? khId = (KhachHangSearchBox.SelectedKhachHang as KhachHangDto)?.Id;

                SetGptBusy(true, isImage ? "Đang phân tích ảnh..." : "Đang phân tích văn bản...");

                // await Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Render);

                var (hd, raw, preds) = await _quick.BuildHoaDonAsync(
                    input,
                    isImage: isImage,
                    khachHangId: khId,
                    customerNameHint: latestCustomerName
                );

                var parsed = hd ?? new HoaDonDto { ChiTietHoaDons = new() };

                Model.ChiTietHoaDons.Clear();

                foreach (var ct in parsed.ChiTietHoaDons)
                {
                    AttachLineWatcher(ct);
                    Model.ChiTietHoaDons.Add(ct);
                }

                UpdateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show("GPT lỗi: " + ex.Message);
            }
            finally
            {
                SetGptBusy(false);
            }
        }
    }
}