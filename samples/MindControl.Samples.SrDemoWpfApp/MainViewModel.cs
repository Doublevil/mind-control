using MindControl.Samples.SlimeRancherDemo;

namespace MindControl.Samples.SrDemoWpfApp;

public class MainViewModel : ViewModelBase
{
    private readonly SlimeRancherDemo.SlimeRancherDemo _slimeRancherDemo = new();

    private readonly System.Timers.Timer _stateUpdateTimer = new(TimeSpan.FromMilliseconds(100));
    
    /// <summary>Gets the view model holding the game state.</summary>
    public GameStateViewModel GameStateVm { get; } = new();
    
    public MainViewModel()
    {
        _stateUpdateTimer.Elapsed += OnStateUpdateTimerTick;
        _stateUpdateTimer.Start();
        GameStateVm.PlayerStateModified += OnGamePlayerStateModified;
        GameStateVm.InfiniteStaminaToggled += OnInfiniteStaminaToggled;
    }

    /// <summary>Event callback. Called when users toggle infinite stamina.</summary>
    private void OnInfiniteStaminaToggled(object? sender, EventArgs e) => _slimeRancherDemo.ToggleInfiniteStamina();

    /// <summary>Event callback. Called when users modify the player state.</summary>
    private void OnGamePlayerStateModified(object? sender, PlayerState e) => _slimeRancherDemo.SetPlayerState(e);

    /// <summary>Called when the refresh timer ticks to update the game state in the game state view model.</summary>
    private void OnStateUpdateTimerTick(object? sender, EventArgs e)
    {
        var gameState = _slimeRancherDemo.GetGameState();
        GameStateVm.UpdateState(gameState);
    }
}