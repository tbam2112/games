using WebGames.Api;
using GamesCore.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<IWordProvider, DictionaryWordProvider>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(2);
});
builder.Services.AddSingleton<WordGameService>();

// builder.Services.ConfigureHttpJsonOptions(options =>
// {
//     options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
// });

var app = builder.Build();

app.UseDefaultFiles(); // must come BEFORE UseStaticFiles — maps "/" to "index.html"
app.UseStaticFiles(); // serves wwwroot/index.html — that's where the frontend will live

app.MapWordGameEndpoints();

app.Run();