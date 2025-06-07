using System.Timers;
using MindControl.Samples.SlimeRancherDemo;

namespace MindControl.Samples.SrDemoBlazorApp;

/// <summary>
/// A service built on top of the <see cref="SlimeRancherDemo"/> class, that periodically refreshes the game state.
/// This service is designed to be instanciated as a singleton and then used by multiple pages & components in the app.
/// </summary>
public class SlimeRancherDemoService
{
    private readonly SlimeRancherDemo.SlimeRancherDemo _slimeRancher;
    private readonly System.Timers.Timer _stateUpdateTimer;
    
    /// <summary>Gets the current game state, refreshed periodically.</summary>
    public GameState CurrentGameState { get; private set; } = new(GameRunState.NotRunning, null);
    public event EventHandler<GameState>? GameStateUpdated;
    
    public SlimeRancherDemoService()
    {
        _slimeRancher = new SlimeRancherDemo.SlimeRancherDemo();
        _stateUpdateTimer = new System.Timers.Timer(TimeSpan.FromMilliseconds(100));
        _stateUpdateTimer.Elapsed += OnStateUpdateTimerTick;
        _stateUpdateTimer.Start();
    }

    /// <summary>Callback for the timer. Refreshes the game state.</summary>
    private void OnStateUpdateTimerTick(object? sender, ElapsedEventArgs e)
    {
        CurrentGameState = _slimeRancher.GetGameState();
        GameStateUpdated?.Invoke(this, CurrentGameState);
    }

    /// <summary>Writes the given player state to memory.</summary>
    /// <param name="playerState">The player state to write.</param>
    public bool SetPlayerState(PlayerState playerState) => _slimeRancher.SetPlayerState(playerState);

    /// <summary>Enables or disables infinite stamina.</summary>
    public void ToggleInfiniteStamina() => _slimeRancher.ToggleInfiniteStamina();
}
