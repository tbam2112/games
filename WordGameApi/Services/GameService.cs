using System.Collections.Concurrent;
using WordGameApi.Models;

namespace WordGameApi.Services;

public class GameService
{
    private readonly ConcurrentDictionary<Guid, Game> _games = new();
    private readonly IWordProvider _wordProvider;

    public GameService(IWordProvider wordProvider)
    {
        _wordProvider = wordProvider;
    }

    public async Task<Game> StartGameAsync(int wordLength, CancellationToken ct = default)
    {
        if (wordLength < 3 || wordLength > 8)
        {
            throw new ArgumentOutOfRangeException(nameof(wordLength), "Word length must be between 3 and 8.");
        }

        var word = await _wordProvider.GetRandomWordAsync(wordLength, ct);

        var game = new Game
        {
            WordLength = wordLength,
            TargetWord = word.ToLowerInvariant(),
            MaxAttempts = 6
        };

        _games[game.Id] = game;
        return game;
    }

    public Game? GetGame(Guid id) => _games.TryGetValue(id, out var game) ? game : null;

    public async Task<(bool success, string? error, LetterResult[]? results)> SubmitGuessAsync(
        Guid gameId, string guess, CancellationToken ct = default)
    {
        if (!_games.TryGetValue(gameId, out var game))
        {
            return (false, "Game not found.", null);
        }

        if (game.Status != GameStatus.InProgress)
        {
            return (false, "Game is already over.", null);
        }

        guess = guess.Trim().ToLowerInvariant();

        if (guess.Length != game.WordLength)
        {
            return (false, $"Guess must be {game.WordLength} letters long.", null);
        }

        if (!guess.All(char.IsLetter))
        {
            return (false, "Guess must contain only letters.", null);
        }

        var isReal = await _wordProvider.IsRealWordAsync(guess, ct);
        if (!isReal)
        {
            return (false, "Not a recognized word.", null);
        }

        var results = ScoreGuess(guess, game.TargetWord);

        game.Guesses.Add(guess);
        game.Results.Add(results);

        if (guess == game.TargetWord)
        {
            game.Status = GameStatus.Won;
        }
        else if (game.Guesses.Count >= game.MaxAttempts)
        {
            game.Status = GameStatus.Lost;
        }

        return (true, null, results);
    }

    /// <summary>
    /// Standard Wordle scoring: exact-position matches are claimed first,
    /// then remaining letters are matched against remaining (unclaimed)
    /// target letters so duplicates are scored correctly.
    /// e.g. target "apple", guess "lllll" -> only the 'l' at index 3 is Correct,
    /// the rest are Absent because there's only one 'l' in "apple".
    /// </summary>
    private static LetterResult[] ScoreGuess(string guess, string target)
    {
        var length = target.Length;
        var results = new LetterResult[length];
        var targetLetterCounts = new Dictionary<char, int>();

        // Pass 1: exact matches, and tally remaining target letters
        for (int i = 0; i < length; i++)
        {
            if (guess[i] == target[i])
            {
                results[i] = LetterResult.Correct;
            }
            else
            {
                targetLetterCounts[target[i]] = targetLetterCounts.GetValueOrDefault(target[i]) + 1;
            }
        }

        // Pass 2: present-but-wrong-position, limited by remaining counts
        for (int i = 0; i < length; i++)
        {
            if (results[i] == LetterResult.Correct) continue;

            if (targetLetterCounts.TryGetValue(guess[i], out var count) && count > 0)
            {
                results[i] = LetterResult.Present;
                targetLetterCounts[guess[i]] = count - 1;
            }
            else
            {
                results[i] = LetterResult.Absent;
            }
        }

        return results;
    }
}
