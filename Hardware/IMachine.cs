namespace MachineSim.Hardware;

/// <summary>
/// Abstraction de la machine. Le ViewModel ne connaît QUE cette interface :
/// il ne sait pas s'il parle à un simulateur ou à une vraie machine.
/// → testable (on peut injecter une fausse machine dans les tests unitaires).
/// </summary>
public interface IMachine
{
    MachineState State { get; }

    /// <summary>Connexion à la machine (lente : ~2 s).</summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// Exécute le programme d'usinage (~10 s). Annulable via le token.
    /// Peut s'interrompre sur une alarme (State passe alors à Alarm).
    /// </summary>
    Task RunProgramAsync(CancellationToken ct);

    /// <summary>Lecture instantanée de la position courante (thread-safe).</summary>
    Position ReadPosition();

    void AcknowledgeAlarm();

    /// <summary>
    /// ⚠️ Déclenché depuis un thread de fond, PAS depuis le thread UI.
    /// </summary>
    event EventHandler<Alarm>? AlarmRaised;
}
