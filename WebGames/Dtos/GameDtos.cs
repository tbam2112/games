using GamesCore.Models;

namespace WebGames.Dtos;

// Difficulty: 1 (easy common words) to 5 (hard rare words)
// Language: "en" for English, "es" for Spanish
public record StartGameRequest(int WordLength, int Difficulty = 1, string Language = "en");

public record StartGameResponse(Guid GameId, int WordLength, int MaxAttempts);

public record GuessRequest(string Guess);

public record GuessResponse(
    LetterResult[] Results,
    GameStatus Status,
    int AttemptsUsed,
    int MaxAttempts,
    string? TargetWord
);

public record GameStateResponse(
    Guid GameId,
    int WordLength,
    int MaxAttempts,
    GameStatus Status,
    List<string> Guesses,
    List<LetterResult[]> Results
);