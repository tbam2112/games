using System.Collections.ObjectModel;
using System.Windows.Input;
using GamesCore.Models;
using GamesCore.Services;

namespace MobileGames.ViewModels;

public record DifficultyOption(string Name, int Value)
{
	// Picker shows ToString() when no ItemDisplayBinding is set
	public override string ToString() => Name;
}

// Drives the Word Game page: setup -> playing -> game over. All game rules
// (scoring, validation, win/loss) come from GamesCore's WordGameService,
// running on the device — this class only tracks what's on screen.
public class WordGameViewModel : ObservableObject
{
	private const double MaxTileSize = 56;
	private const double MaxKeyWidth = 44;
	private const double KeySpacingValue = 4;

	private static readonly string[][] KeyboardLayout =
	{
		new[] { "q", "w", "e", "r", "t", "y", "u", "i", "o", "p" },
		new[] { "a", "s", "d", "f", "g", "h", "j", "k", "l" },
		new[] { KeyViewModel.Enter, "z", "x", "c", "v", "b", "n", "m", KeyViewModel.Backspace },
	};

	private readonly WordGameService _games;
	private readonly Dictionary<string, KeyViewModel> _keys = new();
	private readonly Command _startCommand;

	private Guid _gameId;
	private int _activeLength;
	private int _currentCol;
	private bool _acceptingInput;
	private double _availableWidth = 360;

	private double _wordLength = 5;
	private DifficultyOption _selectedDifficulty;
	private bool _isPlaying;
	private bool _isGameOver;
	private bool _isBusy;
	private string _message = "";
	private string _setupError = "";
	private string _resultHeading = "";
	private string _resultWord = "";
	private int _wins;
	private int _losses;
	private double _tileSpacing = 6;

	public WordGameViewModel(WordGameService games, IWordProvider wordProvider)
	{
		_games = games;

		_startCommand = new Command(async () => await StartGameAsync(), () => !IsBusy);
		ChangeSettingsCommand = new Command(ShowSetup);
		var pressCommand = new Command<string>(PressKey);

		KeyboardRows = KeyboardLayout
			.Select(row => new KeyboardRowViewModel(row.Select(key =>
			{
				var vm = new KeyViewModel(key, pressCommand);
				_keys[key] = vm;
				return vm;
			})))
			.ToList();

		_selectedDifficulty = Difficulties[0];
		UpdateLayoutMetrics();

		// The guess dictionary (~211k words) loads lazily on first lookup —
		// warm it off the UI thread so the first guess doesn't stall.
		_ = Task.Run(() => wordProvider.IsRealWord("warmup"));
	}

	// --- Setup ---

	public IReadOnlyList<DifficultyOption> Difficulties { get; } = new[]
	{
		new DifficultyOption("Easy", 1),
		new DifficultyOption("Medium", 3),
		new DifficultyOption("Hard", 5),
	};

	// double because Stepper.Value is a double
	public double WordLength
	{
		get => _wordLength;
		set => SetProperty(ref _wordLength, Math.Clamp(Math.Round(value), 3, 15));
	}

	public DifficultyOption SelectedDifficulty
	{
		get => _selectedDifficulty;
		set => SetProperty(ref _selectedDifficulty, value ?? Difficulties[0]);
	}

	public string SetupError { get => _setupError; private set => SetProperty(ref _setupError, value); }

	public bool IsBusy
	{
		get => _isBusy;
		private set
		{
			if (SetProperty(ref _isBusy, value)) _startCommand.ChangeCanExecute();
		}
	}

	// --- Screens ---

	public bool IsPlaying
	{
		get => _isPlaying;
		private set
		{
			if (SetProperty(ref _isPlaying, value)) OnPropertyChanged(nameof(IsSetupVisible));
		}
	}

	public bool IsSetupVisible => !IsPlaying;

	public bool IsGameOver
	{
		get => _isGameOver;
		private set
		{
			if (SetProperty(ref _isGameOver, value)) OnPropertyChanged(nameof(IsKeyboardVisible));
		}
	}

	public bool IsKeyboardVisible => !IsGameOver;

	// --- Board and keyboard ---

	public ObservableCollection<RowViewModel> Rows { get; } = new();
	public IReadOnlyList<KeyboardRowViewModel> KeyboardRows { get; }
	public double TileSpacing { get => _tileSpacing; private set => SetProperty(ref _tileSpacing, value); }
	public double KeySpacing => KeySpacingValue;

	public string Message { get => _message; private set => SetProperty(ref _message, value); }

	// --- Result ---

	public string ResultHeading { get => _resultHeading; private set => SetProperty(ref _resultHeading, value); }
	public string ResultWord { get => _resultWord; private set => SetProperty(ref _resultWord, value); }

	public string Record => $"Wins: {_wins}   Losses: {_losses}";

	// --- Commands ---

	// Starts a game with the current settings; also used by "Play again"
	public ICommand StartCommand => _startCommand;
	public ICommand ChangeSettingsCommand { get; }

	// Called by the page whenever its width changes (first layout, rotation)
	public void SetAvailableWidth(double width)
	{
		if (width <= 0 || Math.Abs(width - _availableWidth) < 0.5) return;

		_availableWidth = width;
		UpdateLayoutMetrics();
	}

	public async Task StartGameAsync()
	{
		if (IsBusy) return;

		IsBusy = true;
		SetupError = "";

		try
		{
			var game = await _games.StartGameAsync((int)WordLength, SelectedDifficulty.Value);

			_gameId = game.Id;
			_activeLength = game.WordLength;
			_currentCol = 0;

			foreach (var key in _keys.Values) key.State = TileState.Empty;
			Rows.Clear();
			UpdateLayoutMetrics();
			AddRow();

			Message = "";
			IsGameOver = false;
			IsPlaying = true;
			_acceptingInput = true;
		}
		catch (Exception ex)
		{
			SetupError = $"Could not start a game: {ex.Message}";
		}
		finally
		{
			IsBusy = false;
		}
	}

	public void PressKey(string key)
	{
		if (!_acceptingInput) return;

		switch (key)
		{
			case KeyViewModel.Enter:
				SubmitGuess();
				break;
			case KeyViewModel.Backspace:
				DeleteLetter();
				break;
			default:
				if (key.Length == 1 && char.IsAsciiLetterLower(key[0])) TypeLetter(key);
				break;
		}
	}

	private RowViewModel CurrentRow => Rows[^1];

	private void TypeLetter(string letter)
	{
		if (_currentCol >= _activeLength) return; // row is full

		var tile = CurrentRow.Tiles[_currentCol];
		tile.Letter = letter.ToUpperInvariant();
		tile.State = TileState.Filled;

		_currentCol++;
		HighlightActiveTile();
	}

	private void DeleteLetter()
	{
		if (_currentCol <= 0) return;

		_currentCol--;

		var tile = CurrentRow.Tiles[_currentCol];
		tile.Letter = "";
		tile.State = TileState.Empty;

		HighlightActiveTile();
	}

	private void HighlightActiveTile()
	{
		foreach (var tile in CurrentRow.Tiles)
		{
			if (tile.State == TileState.Active) tile.State = TileState.Empty;
		}

		if (_currentCol < _activeLength)
		{
			CurrentRow.Tiles[_currentCol].State = TileState.Active;
		}
	}

	private void SubmitGuess()
	{
		if (_currentCol < _activeLength)
		{
			Message = $"Word must be {_activeLength} letters.";
			return;
		}

		var guess = string.Concat(CurrentRow.Tiles.Select(t => t.Letter)).ToLowerInvariant();
		var (success, error, results) = _games.SubmitGuess(_gameId, guess);

		if (!success)
		{
			Message = error ?? "Invalid guess.";
			return;
		}

		Message = "";

		for (int i = 0; i < results!.Length; i++)
		{
			var state = TileStates.From(results[i]);
			CurrentRow.Tiles[i].State = state;

			// Keys keep their best result so far (Correct beats Present beats Absent)
			var key = _keys[guess[i].ToString()];
			if (state > key.State) key.State = state;
		}

		var game = _games.GetGame(_gameId)!;

		if (game.Status == GameStatus.InProgress)
		{
			_currentCol = 0;
			AddRow();
			return;
		}

		EndGame(game);
	}

	private void EndGame(Game game)
	{
		_acceptingInput = false;

		if (game.Status == GameStatus.Won)
		{
			_wins++;
			ResultHeading = "You won! 🎉";
		}
		else
		{
			_losses++;
			ResultHeading = "You lost.";
		}

		ResultWord = $"The word was: {game.TargetWord.ToUpperInvariant()}";
		OnPropertyChanged(nameof(Record));
		IsGameOver = true;
	}

	private void ShowSetup()
	{
		_acceptingInput = false;
		IsPlaying = false;
	}

	private void AddRow()
	{
		Rows.Add(new RowViewModel(_activeLength, TileSizeFor(_activeLength), TileSpacing));
		HighlightActiveTile();
	}

	// Tiles and keys shrink to fit the screen so a 15-letter row still fits
	private void UpdateLayoutMetrics()
	{
		var length = _activeLength > 0 ? _activeLength : (int)WordLength;
		TileSpacing = SpacingFor(length);

		var tileSize = TileSizeFor(length);
		foreach (var tile in Rows.SelectMany(r => r.Tiles)) tile.Size = tileSize;

		// Widest keyboard row: 10 letter keys, or 7 letters + 2 keys at 1.5x width
		var keyWidth = Math.Min(MaxKeyWidth, Math.Floor((_availableWidth - 9 * KeySpacingValue) / 10));
		foreach (var key in _keys.Values) key.Width = key.IsWide ? Math.Floor(keyWidth * 1.5) : keyWidth;
	}

	private double TileSizeFor(int length)
	{
		var fit = Math.Floor((_availableWidth - (length - 1) * SpacingFor(length)) / length);
		return Math.Clamp(fit, 16, MaxTileSize);
	}

	// Long words get tighter spacing so the tiles can stay bigger
	private static double SpacingFor(int length) => length > 10 ? 3 : 6;
}
