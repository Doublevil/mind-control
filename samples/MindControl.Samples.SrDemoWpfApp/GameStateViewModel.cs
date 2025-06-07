using MindControl.Samples.SlimeRancherDemo;

namespace MindControl.Samples.SrDemoWpfApp;

public class GameStateViewModel : ViewModelBase
{
    private bool _isUpdatingState = true;
    private PlayerState _latestPlayerState;
    
    public event EventHandler<PlayerState>? PlayerStateModified;
    public event EventHandler? InfiniteStaminaToggled; 
    
    private float _currentHealth;
    private float _currentStamina;
    private int _coinCount;
    private float _maxHealth;
    private float _maxStamina;
    private GameRunState _state;
    
    public float CurrentHealth
    {
        get => _currentHealth;
        set
        {
            SetProperty(ref _currentHealth, value);
            if (!_isUpdatingState)
                RaisePlayerStateModified();
        }
    }

    public float CurrentStamina
    {
        get => _currentStamina;
        set
        {
            SetProperty(ref _currentStamina, value);
            if (!_isUpdatingState)
                RaisePlayerStateModified();
        }
    }

    public int CoinCount
    {
        get => _coinCount;
        set
        {
            SetProperty(ref _coinCount, value);
            if (!_isUpdatingState)
                RaisePlayerStateModified();
        }
    }

    public float MaxHealth { get => _maxHealth; set => SetProperty(ref _maxHealth, value); }
    public float MaxStamina { get => _maxStamina; set => SetProperty(ref _maxStamina, value); }
    public GameRunState State { get => _state; set => SetProperty(ref _state, value); }
    
    /// <summary>Updates the view model to match the given game state.</summary>
    /// <param name="gameState">New game state to sync up with.</param>
    public void UpdateState(GameState gameState)
    {
        State = gameState.State;
        if (gameState.Player == null)
            return;

        _latestPlayerState = gameState.Player.GetValueOrDefault();
        _isUpdatingState = true; // Prevent the view model from raising the StateModified event while updating.
        CurrentHealth = gameState.Player.Value.CurrentHealth;
        CurrentStamina = gameState.Player.Value.CurrentStamina;
        CoinCount = gameState.Player.Value.CoinCount;
        MaxHealth = gameState.Player.Value.MaxHealth;
        MaxStamina = gameState.Player.Value.MaxStamina;
        _isUpdatingState = false;
    }
    
    /// <summary>Raises the <see cref="PlayerStateModified"/> event.</summary>
    private void RaisePlayerStateModified()
    {
        PlayerStateModified?.Invoke(this, _latestPlayerState with
        {
            CurrentHealth = CurrentHealth,
            CurrentStamina = CurrentStamina,
            CoinCount = CoinCount
        });
    }

    /// <summary>Raises the <see cref="InfiniteStaminaToggled"/> event.</summary>
    public void ToggleInfiniteStamina() => InfiniteStaminaToggled?.Invoke(this, EventArgs.Empty);
}