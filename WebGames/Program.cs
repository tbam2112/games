using WebGames.Api;
using GamesCore.Services;
using WebGames.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IWordProvider, DictionaryWordProvider>();
builder.Services.AddSingleton<GamesCore.Services>();

var app = builder.Build();

app.UseStaticFiles(); // serves wwwroot/index.html — that's where the frontend will live

app.MapWordGameEndpoints();

app.Run();