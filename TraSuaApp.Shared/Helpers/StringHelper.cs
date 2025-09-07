namespace TraSuaApp.Shared.Helpers;

public static class StringHelper
{
    public static string? CapitalizeEachWord(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var words = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
        {
            var w = words[i];
            words[i] = char.ToUpper(w[0]) + w.Substring(1).ToLower();
        }
        return string.Join(" ", words);
    }

    public static void NormalizeAllStrings<T>(T obj)
    {
        if (obj == null) return;

        var stringProps = obj.GetType()
            .GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

        foreach (var prop in stringProps)
        {
            var value = prop.GetValue(obj) as string;
            if (value != null)
                prop.SetValue(obj, CapitalizeEachWord(value));
        }
    }
}