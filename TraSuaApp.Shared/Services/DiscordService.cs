using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TraSuaApp.Shared.Enums;

namespace TraSuaApp.Shared.Services
{
    public static class DiscordService
    {
        private static readonly string AdminWebhookUrl = "https://discord.com/api/webhooks/1415629639992217670/RUDcNljSV_thvgiX0eCftW3E7e6u_8pDtHhULWtbvHlZSwLI39NzayQul9XHFDMNbgFA";
        private static readonly string ChuaDungWebhookUrl = "https://discord.com/api/webhooks/1385632148387533011/MmRNpkKCoslZwNO2F9uJd_ZCjiaSvXMKeIpQlDP7gpDBwk1HZt1g2nonmEUiOVITaK0H";
        private static readonly string NhanDonWebhookUrl = "https://discord.com/api/webhooks/1407602585657147452/e6_AL5mYNU4-z6xrlgSKJHkE-YxdOmz4kgKSUMf5WRqPViN4jX1pWN6kXHMYbFKS2dpG";

        private static readonly string HoaDonDelWebhookUrl = "https://discord.com/api/webhooks/1407602584285351998/S7UaKM6Ag0SydXy62x4Y8BWUFJlG0x3_m2cjIUBGfNIX6xjYPWnDhCgvIg5y-t-Hkcoa";

        private static readonly string GhiNoWebhookUrl = "https://discord.com/api/webhooks/1407602329934499971/qWF8nFTIPuixt4Pfc_6Zwf2almtNbPLO1JlBABWQU1LvxW3yxkie2xDz6H9zDpVyBoJi";

        private static readonly string DangGiaoHangWebhookUrl =
            "https://discord.com/api/webhooks/1406106727459586210/jsHzaUiUitSTCx_6jRoLNQd2d5SzKgkoNsd0P0ruNd58kIGh0lDsaFPTFiuM-GkUBjcH";

        private static readonly string ThanhToanWebhookUrl =
            "https://discord.com/api/webhooks/1406254048457527318/mSo2U82BGl01R_eUqRT33TUMYaiVKh6bI5eMcfiAz4MLc8UGliB1pyzhegdQbgZvxqSO";

        private static readonly string HoaDonNewWebhookUrl = "https://discord.com/api/webhooks/1407923073876758671/h0u62xhceFI3zf2RxX52NapcvqpdZt76Hb1hpelsVJmSzuxj218CKEFD5E6nnDqaEnAN";

        private static readonly string HenGioWebhookUrl =
            "https://discord.com/api/webhooks/1407602320656695387/lFpXgcQ3SS2GrDvlNI_IqY6mCHHNvdbqYOFvajKE2l-4KmVQWaJNf1qTul5LToUOKJd1";

        private static readonly string TraNoWebhookUrl = "https://discord.com/api/webhooks/1407923083070935162/ApVIOUwIJuOKxX1YSC1ntg-b3pQkcKQmByq51A-X68Qtppnyfz0FAmw_68a62K2kj58L";
        private static readonly string DaGiaoHangWebhookUrl = "https://discord.com/api/webhooks/1407602585048715355/lIYnieakxN_uILGkgDlMXHK4CJtz9CKcIrryylABBUYJJbkGfux6wpqrxorMtMC2s2cP";

        public static Task SendAsync(DiscordEventType eventType, string message)
        {
            string url = eventType switch
            {
                DiscordEventType.Admin => AdminWebhookUrl,
                DiscordEventType.DuyKhanh => DaGiaoHangWebhookUrl,
                DiscordEventType.DangGiaoHang => DangGiaoHangWebhookUrl,
                DiscordEventType.NhanDon => NhanDonWebhookUrl,
                DiscordEventType.HenGio => HenGioWebhookUrl,
                DiscordEventType.ThanhToan => ThanhToanWebhookUrl,
                DiscordEventType.TraNo => TraNoWebhookUrl,
                DiscordEventType.HoaDonNew => HoaDonNewWebhookUrl,
                DiscordEventType.HoaDonDel => HoaDonDelWebhookUrl,
                DiscordEventType.GhiNo => GhiNoWebhookUrl,
                _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null)
            };
            return SendToWebhook(url, message);
        }
        public static Task SendAsync(DiscordEventType eventType, string message, string fileNameIfTooLong)
        {
            string url = eventType switch
            {
                DiscordEventType.Admin => AdminWebhookUrl,
                DiscordEventType.DuyKhanh => DaGiaoHangWebhookUrl,
                DiscordEventType.DangGiaoHang => DangGiaoHangWebhookUrl,
                DiscordEventType.NhanDon => NhanDonWebhookUrl,
                DiscordEventType.HenGio => HenGioWebhookUrl,
                DiscordEventType.ThanhToan => ThanhToanWebhookUrl,
                DiscordEventType.TraNo => TraNoWebhookUrl,
                DiscordEventType.HoaDonNew => HoaDonNewWebhookUrl,
                DiscordEventType.HoaDonDel => HoaDonDelWebhookUrl,
                DiscordEventType.GhiNo => GhiNoWebhookUrl,
                _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null)
            };

            return SendToWebhook(url, message, fileNameIfTooLong);
        }
        private static async Task SendToWebhook(string url, string message, string fileNameIfTooLong = "AllFiles.txt")
        {
            using var client = new HttpClient();

            if (message.Length < 1900)
            {
                var payload = new
                {
                    content = message
                };

                var json = JsonSerializer.Serialize(payload);
                await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            }
            else
            {
                var contentToSend = new MultipartFormDataContent();
                var bytes = Encoding.UTF8.GetBytes(message);
                var fileContent = new ByteArrayContent(bytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

                contentToSend.Add(fileContent, "file", fileNameIfTooLong);

                await client.PostAsync(url, contentToSend);
            }
        }
    }
}