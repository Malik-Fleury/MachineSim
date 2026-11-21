namespace MachineSim.Hardware;

public enum MachineState
{
    Disconnected,
    Idle,
    Running,
    Alarm
}

/// <summary>Position de l'outil en mm.</summary>
public readonly record struct Position(double X, double Y, double Z);

public record Alarm(DateTime Timestamp, int Code, string Message)
{
    // Utilisé par la ListBox pour afficher l'alarme (bonus : remplacer par un DataTemplate).
    public override string ToString() => $"{Timestamp:HH:mm:ss}  [{Code}]  {Message}";
}
