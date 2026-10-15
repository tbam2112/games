using System.Collections.ObjectModel;
using System.Windows.Input;
using GamesCore.Models;

namespace MobileGames.ViewModels;

// Shared by board tiles and keyboard keys. Order matters: a key keeps the
// best result it's seen, and Absent < Present < Correct.
public enum TileState
{
	Empty,
	Active,  // the tile the next letter goes into
	Filled,  // typed but not yet submitted
	Absent,
	Present,
	Correct
}

public static class TileStates
{
	public static TileState From(LetterResult result) => result switch
	{
		LetterResult.Correct => TileState.Correct,
		LetterResult.Present => TileState.Present,
		_ => TileState.Absent,
	};
}

public class TileViewModel : ObservableObject
{
	private string _letter = "";
	private TileState _state;
	private double _size;

	public string Letter { get => _letter; set => SetProperty(ref _letter, value); }
	public TileState State { get => _state; set => SetProperty(ref _state, value); }
	public double Size
	{
		get => _size;
		set
		{
			if (SetProperty(ref _size, value)) OnPropertyChanged(nameof(FontSize));
		}
	}

	public double FontSize => Math.Max(10, Math.Round(Size * 0.55));
}

public class RowViewModel
{
	public RowViewModel(int length, double tileSize, double spacing)
	{
		Spacing = spacing;
		for (int i = 0; i < length; i++)
		{
			Tiles.Add(new TileViewModel { Size = tileSize });
		}
	}

	public ObservableCollection<TileViewModel> Tiles { get; } = new();
	public double Spacing { get; }
}

public class KeyViewModel : ObservableObject
{
	public const string Enter = "enter";
	public const string Backspace = "backspace";

	private TileState _state;
	private double _width;

	public KeyViewModel(string key, ICommand pressCommand)
	{
		Key = key;
		PressCommand = pressCommand;
	}

	// A lowercase letter, or Enter / Backspace
	public string Key { get; }
	public ICommand PressCommand { get; }
	public bool IsWide => Key is Enter or Backspace;

	public string Label => Key switch
	{
		Enter => "Enter",
		Backspace => "⌫",
		_ => Key.ToUpperInvariant(),
	};

	public TileState State { get => _state; set => SetProperty(ref _state, value); }
	public double Width { get => _width; set => SetProperty(ref _width, value); }
}

public class KeyboardRowViewModel
{
	public KeyboardRowViewModel(IEnumerable<KeyViewModel> keys)
	{
		Keys = keys.ToList();
	}

	public IReadOnlyList<KeyViewModel> Keys { get; }
}
