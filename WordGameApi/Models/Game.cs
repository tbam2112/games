namespace WordGameApi.Models;

public enum LetterResult
{
    Absent,   // letter not in the word
    Present,  // letter in the word, wrong position
    Correct   // letter in the word, right position
}

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
    public int MaxAttempts { get; init; } = 6;
    public List<string> Guesses { get; } = new();
    public List<LetterResult[]> Results { get; } = new();
    public GameStatus Status { get; set; } = GameStatus.InProgress;
}
