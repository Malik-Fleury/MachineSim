using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MachineSim.Mvvm;

/// <summary>
/// Classe de base de tous les ViewModels : implémente INotifyPropertyChanged
/// pour que les bindings WPF soient prévenus des changements.
/// (En prod on utiliserait CommunityToolkit.Mvvm, mais ici on le code soi-même pour comprendre.)
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Prévient les abonnés (les bindings) qu'une propriété a changé.
    /// [CallerMemberName] remplit automatiquement le nom de la propriété appelante.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Met à jour un champ et notifie SEULEMENT si la valeur a réellement changé.
    /// Retourne true si la valeur a changé (utile pour déclencher d'autres actions).
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        bool equals = !EqualityComparer<T>.Default.Equals(field, value);

        if (equals)
        {
            field = value;
            OnPropertyChanged(propertyName);
        }
        
        return equals;
    }
}
