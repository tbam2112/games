using System.Collections.Specialized;
using MobileGames.ViewModels;

namespace MobileGames.Pages;

public partial class WordGamePage : ContentPage
{
	private const string ThemePreferenceKey = "theme";

	private readonly WordGameViewModel _viewModel;

	public WordGamePage(WordGameViewModel viewModel)
	{
		InitializeComponent();

		_viewModel = viewModel;
		BindingContext = viewModel;

		// Keep the newest row in view when a long game outgrows the screen
		viewModel.Rows.CollectionChanged += OnRowsChanged;

		ApplySavedTheme();

		// With no override saved, the icon follows OS theme changes too
		Application.Current!.RequestedThemeChanged += (_, _) => UpdateThemeToggle();
	}

	private void OnGameAreaSizeChanged(object? sender, EventArgs e)
	{
		if (sender is VisualElement area) _viewModel.SetAvailableWidth(area.Width);
	}

	private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action != NotifyCollectionChangedAction.Add) return;

		// Let the new row lay out before scrolling to it
		Dispatcher.Dispatch(async () => await BoardScroll.ScrollToAsync(0, Board.Height, animated: true));
	}

	// --- Theme: follows the OS by default; the toolbar button stores an override ---

	private void ApplySavedTheme()
	{
		var saved = Preferences.Default.Get(ThemePreferenceKey, "");
		Application.Current!.UserAppTheme = saved switch
		{
			"light" => AppTheme.Light,
			"dark" => AppTheme.Dark,
			_ => AppTheme.Unspecified,
		};

		UpdateThemeToggle();
	}

	private void OnThemeToggleClicked(object? sender, EventArgs e)
	{
		var next = Application.Current!.RequestedTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
		Application.Current.UserAppTheme = next;
		Preferences.Default.Set(ThemePreferenceKey, next == AppTheme.Dark ? "dark" : "light");

		UpdateThemeToggle();
	}

	private void UpdateThemeToggle()
	{
		ThemeToggle.Text = Application.Current!.RequestedTheme == AppTheme.Dark ? "☀️" : "🌙";
	}
}
