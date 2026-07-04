namespace GamesCore.Models;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LetterResult
{
    Absent,
    Present,
    Correct
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameStatus
{
    InProgress,
    Won,
    Lost
}


public class Game
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int WordLength { get; init; }
    public required string TargetWord { get; init; } // stored lowercase, never sent to client until game ends

    // MaxAttempts is derived from the WordLength; compute at runtime to avoid
    // referencing instance fields in a field initializer.
    public int MaxAttempts => WordLength + 1;
    public List<string> Guesses { get; } = new();
    public List<LetterResult[]> Results { get; } = new();
    public GameStatus Status { get; set; } = GameStatus.InProgress;
}
