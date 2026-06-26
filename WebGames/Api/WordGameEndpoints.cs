using WebGames.Dtos;
using WebGames.Models;
using WebGames.Services;

namespace WebGames.Api;

public static class WordGameEndpoints
{
    public static void MapWordGameEndpoints(this WebApplication app)
    {
        // Start a new game for a given word length (3-8 letters)
        app.MapPost("/api/games", async (StartGameRequest request, WordGameService games) =>
        {
            try
            {
                var game = await games.StartGameAsync(request.WordLength);
                return Results.Ok(new StartGameResponse(game.Id, game.WordLength, game.MaxAttempts));
            }
            catch (ArgumentOutOfRangeException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        });

        // Submit a guess for an in-progress game
        app.MapPost("/api/games/{id}/guess", async (Guid id, GuessRequest request, WordGameService games) =>
        {
            var (success, error, results) = await games.SubmitGuessAsync(id, request.Guess);

            if (!success)
            {
                return Results.BadRequest(new { error });
            }

            var game = games.GetGame(id)!;
            var response = new GuessResponse(
                Results: results!,
                Status: game.Status,
                AttemptsUsed: game.Guesses.Count,
                MaxAttempts: game.MaxAttempts,
                TargetWord: game.Status == GameStatus.InProgress ? null : game.TargetWord
            );

            return Results.Ok(response);
        });

        // Get the current state of a game (e.g. on page refresh)
        app.MapGet("/api/games/{id}", (Guid id, WordGameService games) =>
        {
            var game = games.GetGame(id);
            if (game is null) return Results.NotFound();

            var response = new GameStateResponse(
                GameId: game.Id,
                WordLength: game.WordLength,
                MaxAttempts: game.MaxAttempts,
                Status: game.Status,
                Guesses: game.Guesses,
                Results: game.Results
            );

            return Results.Ok(response);
        });
    }
}