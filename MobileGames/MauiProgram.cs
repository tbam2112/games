using GamesCore.Services;
using Microsoft.Extensions.Logging;
using MobileGames.Pages;
using MobileGames.ViewModels;

namespace MobileGames;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// Game logic runs on the device. The word provider still asks
		// random-word-api for target words when online (2s timeout, same as
		// WebGames) and falls back to its bundled list when offline.
		builder.Services.AddSingleton<IWordProvider>(_ =>
			new DictionaryWordProvider(new HttpClient { Timeout = TimeSpan.FromSeconds(2) }));
		builder.Services.AddSingleton<WordGameService>();
		builder.Services.AddSingleton<WordGameViewModel>();
		builder.Services.AddTransient<WordGamePage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
