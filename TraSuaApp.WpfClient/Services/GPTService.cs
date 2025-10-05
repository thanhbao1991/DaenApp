//using OpenAI.Chat;

//namespace TraSuaApp.WpfClient.Services
//{
//    public class GPTService
//    {
//        private readonly ChatClient _chatClient;

//        public GPTService(string apiKey, string model = "gpt-4o-mini")
//        {
//            _chatClient = new ChatClient(model, apiKey);
//        }

//        public async Task<string> AskAsync(string prompt, string systemPrompt = null)
//        {
//            var messages = new List<ChatMessage>();

//            if (!string.IsNullOrWhiteSpace(systemPrompt))
//                messages.Add(new SystemChatMessage(systemPrompt));

//            messages.Add(new UserChatMessage(prompt));

//            var result = await _chatClient.CompleteChatAsync(messages);
//            ChatCompletion completion = result.Value;

//            return completion.Content[0].Text;
//        }
//    }
//}