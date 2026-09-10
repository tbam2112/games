using System.Reflection;

namespace GamesCore.Services;

// Bundled fallback dictionary (macOS's /usr/share/dict/words, filtered to
// lowercase alphabetic entries) used when the external dictionary API is
// unavailable, so guess validation degrades to "pretty good" instead of
// "everything or nothing."
internal static class LocalWordList
{
    private static readonly Lazy<HashSet<string>> Words = new(Load);

    public static bool Contains(string word) => Words.Value.Contains(word);

    private static HashSet<string> Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .Single(name => name.EndsWith("words.txt", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);

        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length > 0) words.Add(line);
        }

        return words;
    }
}
