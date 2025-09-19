using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc;

namespace TraSuaApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TranscribeController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public TranscribeController(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        [HttpPost]
        [RequestSizeLimit(10_000_000)] // ~10MB
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File rỗng.");

            using var content = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/webm");

            content.Add(streamContent, "file", file.FileName);
            content.Add(new StringContent("whisper-1"), "model");

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config["OpenAI:ApiKey"]);

            var response = await _httpClient.PostAsync(
                "https://api.openai.com/v1/audio/transcriptions", content);

            var json = await response.Content.ReadAsStringAsync();
            return Content(json, "application/json");
        }
    }
}