namespace MachineSim.Hardware;

/// <summary>
/// Simule un centre d'usinage. Ce fichier joue le rôle du "matériel" :
/// il est fourni complet, tu n'as pas à le modifier
/// (sauf pour l'expérience bonus du README sur ConfigureAwait).
/// </summary>
public class SimulatedMachine : IMachine
{
    private static readonly (int Code, string Message)[] PossibleAlarms =
    {
        (101, "Surchauffe broche"),
        (205, "Pression d'huile basse"),
        (312, "Fin de course axe Y"),
        (420, "Rupture d'outil détectée"),
    };

    private readonly Random _random = new();
    private readonly object _lock = new();
    private Position _position = new(50, 50, 0);
    private volatile MachineState _state = MachineState.Disconnected;

    public MachineState State => _state;

    public event EventHandler<Alarm>? AlarmRaised;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        // Simule une connexion lente (bus de terrain, réseau...).
        // ConfigureAwait(false) : code "bibliothèque", pas besoin de revenir sur le thread UI.
        await Task.Delay(2000, ct).ConfigureAwait(false);
        _state = MachineState.Idle;
    }

    public async Task RunProgramAsync(CancellationToken ct)
    {
        if (_state != MachineState.Idle)
            throw new InvalidOperationException($"Démarrage impossible depuis l'état {_state}.");

        _state = MachineState.Running;
        try
        {
            // Trajectoire : un cercle en XY, avec une descente progressive en Z.
            const int steps = 400; // ~10 s à 25 ms par pas
            for (int i = 0; i <= steps; i++)
            {
                ct.ThrowIfCancellationRequested();

                double angle = 2 * Math.PI * i / steps;
                lock (_lock)
                {
                    _position = new Position(
                        50 + 40 * Math.Cos(angle),
                        50 + 40 * Math.Sin(angle),
                        -5.0 * i / steps);
                }

                // Petite chance d'alarme à chaque pas (~45 % de chance par programme).
                if (i > 0 && _random.NextDouble() < 0.0015)
                {
                    RaiseRandomAlarm();
                    return;
                }

                await Task.Delay(25, ct).ConfigureAwait(false);
            }

            _state = MachineState.Idle;
        }
        catch (OperationCanceledException)
        {
            _state = MachineState.Idle; // arrêt demandé par l'opérateur
            throw;
        }
    }

    public Position ReadPosition()
    {
        lock (_lock)
        {
            return _position;
        }
    }

    public void AcknowledgeAlarm()
    {
        if (_state == MachineState.Alarm)
            _state = MachineState.Idle;
    }

    private void RaiseRandomAlarm()
    {
        var (code, message) = PossibleAlarms[_random.Next(PossibleAlarms.Length)];
        _state = MachineState.Alarm;

        // On est ici sur un thread du ThreadPool (à cause du ConfigureAwait(false) plus haut),
        // donc les abonnés à cet événement seront appelés depuis ce thread-là, pas le thread UI.
        AlarmRaised?.Invoke(this, new Alarm(DateTime.Now, code, message));
    }
}
