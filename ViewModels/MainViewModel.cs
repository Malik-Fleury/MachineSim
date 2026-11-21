using System.Collections.ObjectModel;
using System.Reflection.PortableExecutable;
using System.Windows.Data;
using MachineSim.Hardware;
using MachineSim.Mvvm;

namespace MachineSim.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly IMachine _machine;
    private CancellationTokenSource? _pollingCts;
    private CancellationTokenSource? _programCts;
    private readonly object _alarmsLock = new();

    public MainViewModel(IMachine machine)
    {
        _machine = machine;
        _machine.AlarmRaised += OnAlarmRaised;
        
        ConnectCommand = new RelayCommand(
            async () => await ConnectAsync(),
            () => State == MachineState.Disconnected && !IsBusy);
        
        StartCommand = new RelayCommand(async () => await StartProgramAsync(), () => State == MachineState.Idle);
        StopCommand = new RelayCommand(StopProgram, () => State == MachineState.Running);
        AcknowledgeCommand = new RelayCommand(AcknowledgeAlarm, () => State == MachineState.Alarm);

        BindingOperations.EnableCollectionSynchronization(Alarms, _alarmsLock);
    }

    // ─────────────────────────── Commandes ───────────────────────────

    public RelayCommand ConnectCommand { get; }
    public RelayCommand StartCommand { get; }
    public RelayCommand StopCommand { get; }
    public RelayCommand AcknowledgeCommand { get; }

    // ─────────────────────── Propriétés bindées ───────────────────────

    private MachineState _state = MachineState.Disconnected;
    public MachineState State
    {
        get => _state;
        set
        {
            if (SetProperty(ref _state, value))
                RefreshCommands(); // l'état a changé → les CanExecute aussi
        }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
                RefreshCommands();
        }
    }

    private string _statusText = "Déconnecté";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }
    
    private double _positionX = 0.0;
    public double PositionX
    {
        get => _positionX;
        set => SetProperty(ref _positionX, value);
    }
    
    private double _positionY = 0.0;
    public double PositionY
    {
        get => _positionY;
        set => SetProperty(ref _positionY, value);
    }
    
    private double _positionZ = 0.0;
    public double PositionZ
    {
        get => _positionZ;
        set => SetProperty(ref _positionZ, value);
    }

    /// <summary>Historique des alarmes, la plus récente en premier.</summary>
    public ObservableCollection<Alarm> Alarms { get; } = new();

    // ─────────────────────────── Logique ───────────────────────────

    private void RefreshCommands()
    {
        ConnectCommand.NotifyCanExecuteChanged();
        StartCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        AcknowledgeCommand.NotifyCanExecuteChanged();
    }
    
    private async Task ConnectAsync()
    {
        IsBusy = true;
        StatusText = "Connexion en cours...";
        try
        {
            await _machine.ConnectAsync();

            // On est de retour sur le thread UI ici : on peut tout mettre à jour sans Dispatcher.
            State = _machine.State;
            StatusText = "Connecté. Machine prête.";

            _pollingCts = new CancellationTokenSource();
            _ = PollMachineAsync(_pollingCts.Token); // fire-and-forget volontaire (la boucle gère ses erreurs)
        }
        catch (Exception ex)
        {
            StatusText = $"Échec de connexion : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Boucle de rafraîchissement : lit l'état et la position de la machine toutes les 50 ms.
    /// </summary>
    private async Task PollMachineAsync(CancellationToken ct)
    {
        using (var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50)))
        {
            try
            {
                while (await timer.WaitForNextTickAsync(ct))
                {
                    Position position = _machine.ReadPosition();
                    MachineState state = _machine.State;

                    UpdatePosition(position);

                    State = state;
                }
            }
            catch (OperationCanceledException e)
            {
                // Rien... opération normale de l'annulation
            }
        }
    }

    private void UpdatePosition(Position position)
    {
        PositionX = position.X;
        PositionY = position.Y;
        PositionZ = position.Z;
    }
    
    private async Task StartProgramAsync()
    {
        _programCts = new CancellationTokenSource();

        StatusText = "Programme en cours...";
        try
        {
            await _machine.RunProgramAsync(_programCts.Token);

            // On est de retour sur le thread UI ici : on peut tout mettre à jour sans Dispatcher.
            State = _machine.State;
            UpdatePosition(_machine.ReadPosition());
            
            if(_state == MachineState.Idle)
                StatusText = $"Opération terminée";
        }
        catch (OperationCanceledException eCanceled)
        {
            StatusText = $"Opération annulée : {eCanceled.Message}";
        }
        catch (Exception eCanceled)
        {
            StatusText = $"Autre erreur : {eCanceled.Message}";
        }
        finally
        {
            _programCts.Dispose();
            _programCts = null;
        }
        
    }

    private void StopProgram()
    {
        _programCts?.Cancel();
    }

    private void AcknowledgeAlarm()
    {
        _machine.AcknowledgeAlarm();
        StatusText = "Alarme acquittée.";
    }

    /// <summary>
    /// Appelé depuis un THREAD DE FOND par la machine (voir SimulatedMachine.RaiseRandomAlarm).
    /// </summary>
    private void OnAlarmRaised(object? sender, Alarm alarm)
    {
        StatusText = $"ALARME {alarm.Code} : {alarm.Message}";
        
        lock (_alarmsLock)
        {
            Alarms.Insert(0, alarm);
        }
    }
}
