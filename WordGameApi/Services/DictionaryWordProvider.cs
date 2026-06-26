namespace WordGameApi.Services;

public interface IWordProvider
{
    Task<string> GetRandomWordAsync(int length, CancellationToken ct = default);
    Task<bool> IsRealWordAsync(string word, CancellationToken ct = default);
}

/// <summary>
/// Pulls a random word of the requested length from a free public word API,
/// and validates guesses against a free dictionary lookup API.
/// Falls back to a small local word list if the network call fails, so the
/// game still works offline / if the third-party API is down.
/// </summary>
public class DictionaryWordProvider : IWordProvider
{
    private readonly HttpClient _http;
    private readonly Random _random = new();

    // Small local fallback list, grouped by word length. Extend as you like.
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

    public async Task<string> GetRandomWordAsync(int length, CancellationToken ct = default)
    {
        try
        {
            // random-word-api returns a JSON array of words, e.g. ["apple"]
            var url = $"https://random-word-api.herokuapp.com/word?length={length}";
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
            // If validation API is unreachable, don't block play — accept the guess.
            return true;
        }
    }

    private string GetFallbackWord(int length)
    {
        if (FallbackWords.TryGetValue(length, out var list))
        {
            return list[_random.Next(list.Length)];
        }

        // No fallback for this length — pick the closest available length.
        var closest = FallbackWords.Keys.OrderBy(k => Math.Abs(k - length)).First();
        return FallbackWords[closest][_random.Next(FallbackWords[closest].Length)];
    }
}
