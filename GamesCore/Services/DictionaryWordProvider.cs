using System.Net.Http.Json;

namespace GamesCore.Services;

public interface IWordProvider
{
    // difficulty: 1 (easy) to 5 (hard), matches the random-word-api ?diff param
    // language: "en" (English) or "es" (Spanish)
    Task<string> GetRandomWordAsync(int length, int difficulty = 1, string language = "en", CancellationToken ct = default);
    Task<bool> IsRealWordAsync(string word, CancellationToken ct = default);
}

public class DictionaryWordProvider : IWordProvider
{
    private readonly HttpClient _http;
    private readonly Random _random = new();

    private static readonly Dictionary<int, string[]> FallbackWords = new()
    {
        [3] = new[] { "cat", "dog", "sun", "run", "big", "red", "top", "win" },
        [4] = new[] { "code", "game", "word", "play", "blue", "fast", "moon", "tree" },
        [5] = new[] { "apple", "brave", "crane", "dance", "eagle", "flame", "grape", "house" },
        [6] = new[] { "puzzle", "garden", "monkey", "planet", "rocket", "yellow", "bridge" },
        [7] = new[] { "freedom", "amazing", "kitchen", "journey", "diamond", "captain" },
        [8] = new[] { "computer", "elephant", "mountain", "sandwich", "umbrella" },
    };

    public DictionaryWordProvider(HttpClient http)
    {
        _http = http;
    }

    public async Task<string> GetRandomWordAsync(int length, int difficulty = 1, string language = "en", CancellationToken ct = default)
    {
        try
        {
            // Build the URL with optional difficulty and language params.
            // The API only supports ?diff when requesting 5 or fewer words,
            // and ?lang=es for Spanish. English is the default so we only
            // include lang when it's not English.
            var url = $"https://random-word-api.herokuapp.com/word?length={length}&diff={difficulty}";
            if (language != "en")
            {
                url += $"&lang={language}";
            }

            var words = await _http.GetFromJsonAsync<string[]>(url, ct);

            if (words is { Length: > 0 } && words[0].Length == length)
            {
                return words[0].ToLowerInvariant();
            }
        }
        catch
        {
            // network/API unavailable — fall through to local list
        }

        return GetFallbackWord(length);
    }

    public async Task<bool> IsRealWordAsync(string word, CancellationToken ct = default)
    {
        try
        {
            var url = $"https://api.dictionaryapi.dev/api/v2/entries/en/{Uri.EscapeDataString(word)}";
            var response = await _http.GetAsync(url, ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return true;
        }
    }

    private string GetFallbackWord(int length)
    {
        if (FallbackWords.TryGetValue(length, out var list))
        {
            return list[_random.Next(list.Length)];
        }

        var closest = FallbackWords.Keys.OrderBy(k => Math.Abs(k - length)).First();
        return FallbackWords[closest][_random.Next(FallbackWords[closest].Length)];
    }
}