using System.Windows.Input;

namespace MachineSim.Mvvm;

/// <summary>
/// Commande générique : relie un bouton (Command="{Binding ...}") à une méthode du ViewModel.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>
    /// WPF s'abonne à cet événement pour savoir quand réinterroger CanExecute
    /// (et donc griser / dégriser le bouton).
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return _canExecute?.Invoke() ?? true;
    }

    public void Execute(object? parameter) => _execute();

    /// <summary>
    /// À appeler par le ViewModel quand une condition utilisée dans CanExecute a changé.
    /// </summary>
    public void NotifyCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
