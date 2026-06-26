using WebGames.Models;

namespace WebGames.Dtos;

public record StartGameRequest(int WordLength);

public record StartGameResponse(Guid GameId, int WordLength, int MaxAttempts);

public record GuessRequest(string Guess);

public record GuessResponse(
    LetterResult[] Results,
    GameStatus Status,
    int AttemptsUsed,
    int MaxAttempts,
    string? TargetWord // only populated once Status is Won or Lost
);

public record GameStateResponse(
    Guid GameId,
    int WordLength,
    int MaxAttempts,
    GameStatus Status,
    List<string> Guesses,
    List<LetterResult[]> Results
);
