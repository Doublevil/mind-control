namespace MindControl.Samples.SlimeRancherDemo;

/// <summary>
/// Represents the tracked game. Provides methods built on top of the MindControl classes to manipulate the game.
/// </summary>
public class SlimeRancherDemo
{
    private readonly PointerPath _playerStatePath = "UnityPlayer.dll+0168EEA0,8,100,28,20,74";
    private readonly ProcessTracker _processTracker = new("SlimeRancher");

    private IDisposable? _infiniteStaminaFreezer;
    
    public bool IsInfinityStaminaEnabled => _infiniteStaminaFreezer != null;
    
    /// <summary>Gets the current game state.</summary>
    public GameState GetGameState()
    {
        var process = _processTracker.GetProcessMemory();
        if (process == null)
            return new GameState(GameRunState.NotRunning, Player: null);

        var playerStateResult = process.Read<PlayerState>(_playerStatePath);
        if (!playerStateResult.IsSuccess)
            return new GameState(GameRunState.InMenu, Player: null);
        
        return new GameState(GameRunState.InGame, playerStateResult.Value);
    }

    /// <summary>Sets the player state.</summary>
    /// <param name="newState">The new player state.</param>
    public bool SetPlayerState(PlayerState newState)
    {
        var process = _processTracker.GetProcessMemory();
        if (process == null)
            return false;
        
        return process.Write(_playerStatePath, newState).IsSuccess;
    }

    /// <summary>Freezes the stamina value to 100, or unfreezes it if it's already frozen.</summary>
    public bool ToggleInfiniteStamina()
    {
        var process = _processTracker.GetProcessMemory();
        if (process == null)
            return false;

        if (_infiniteStaminaFreezer != null)
        {
            _infiniteStaminaFreezer.Dispose();
            _infiniteStaminaFreezer = null;
        }
        else
            _infiniteStaminaFreezer = process.GetAnchor<float>(_playerStatePath).Freeze(100);

        return true;
    }
}