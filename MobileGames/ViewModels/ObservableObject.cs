using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MobileGames.ViewModels;

// Minimal INotifyPropertyChanged base so the view models don't need an MVVM package.
public abstract class ObservableObject : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
