namespace TraSuaApp.Shared.Helpers
{
    public static class StringHelper
    {
        public static string NormalizeString(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var words = input.Trim()
                             .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                             .Select(CapitalizeFirstLetter);

            return string.Join(' ', words);
        }

        private static string CapitalizeFirstLetter(string word)
        {
            if (string.IsNullOrEmpty(word)) return "";
            return char.ToUpper(word[0]) + word[1..].ToLower();
        }
    }
}