using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TraSuaApp.Shared.Services
{
    public static class DiscordService
    {
        private static readonly string EscWebhookUrl =
            "https://discord.com/api/webhooks/1406106727459586210/jsHzaUiUitSTCx_6jRoLNQd2d5SzKgkoNsd0P0ruNd58kIGh0lDsaFPTFiuM-GkUBjcH";

        private static readonly string ThanhToanWebhookUrl =
            "https://discord.com/api/webhooks/1406254048457527318/mSo2U82BGl01R_eUqRT33TUMYaiVKh6bI5eMcfiAz4MLc8UGliB1pyzhegdQbgZvxqSO";

        private static readonly string NhanDonWebhookUrl =
            "https://discord.com/api/webhooks/1385632148387533011/MmRNpkKCoslZwNO2F9uJd_ZCjiaSvXMKeIpQlDP7gpDBwk1HZt1g2nonmEUiOVITaK0H";

        private static readonly string HenGioWebhookUrl =
            "https://discord.com/api/webhooks/1407602320656695387/lFpXgcQ3SS2GrDvlNI_IqY6mCHHNvdbqYOFvajKE2l-4KmVQWaJNf1qTul5LToUOKJd1";

        private static readonly string TraNoWebhookUrl = "https://discord.com/api/webhooks/1407923083070935162/ApVIOUwIJuOKxX1YSC1ntg-b3pQkcKQmByq51A-X68Qtppnyfz0FAmw_68a62K2kj58L";

        public static Task ThanhToanAsync(string message) =>
            SendAsync(ThanhToanWebhookUrl, message);
        public static Task TraNoAsync(string message) =>
            SendAsync(TraNoWebhookUrl, message);

        public static Task DiShipAsync(string message) =>
            SendAsync(EscWebhookUrl, message);
        public static Task NhanDonAsync(string message) =>
              SendAsync(NhanDonWebhookUrl, message);
        public static Task HenGioAsync(string message) =>
              SendAsync(HenGioWebhookUrl, message);

        private static async Task SendAsync(string webhookUrl, string message)
        {
            using var client = new HttpClient();
            message = $"{message}\n{DateTime.Now.ToString("dd-MM-yyyy hh:mm tt")}\n\u200B";
            if (message.Length < 1900)
            {
                var payload = new { content = message };
                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                await client.PostAsync(webhookUrl, content);
            }
            else
            {
                using var contentToSend = new MultipartFormDataContent();
                var bytes = Encoding.UTF8.GetBytes(message);
                var fileContent = new ByteArrayContent(bytes);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                contentToSend.Add(fileContent, "file", "Error.txt");
                await client.PostAsync(webhookUrl, contentToSend);
            }
        }


    }
}