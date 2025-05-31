using System.Runtime.InteropServices;

namespace MindControl.Samples.SlimeRancherDemo;

/// <summary>Enumerates the potential states that the game can have.</summary>
public enum GameRunState
{
    /// <summary>The game is not running.</summary>
    NotRunning,
    /// <summary>The game is running but no game has been started.</summary>
    InMenu,
    /// <summary>The game is running and a game has been loaded.</summary>
    InGame
}

/// <summary>Represents the current state of the Slime Rancher demo.</summary>
/// <param name="State">The current state of the game.</param>
/// <param name="Player">The current player state.</param>
public record GameState(GameRunState State, PlayerState? Player);

/// <summary>Represents the current state of the player in the Slime Rancher demo.</summary>
/// <remarks>
/// We are using the StructLayout attribute with the "Explicit" value here, which allows us to specify the offset of
/// each field in the structure. This lets us to mimic the actual structure of the game's player state in memory, which
/// means we can read and write the player state directly from/to memory in a single operation.
/// </remarks>
[StructLayout(LayoutKind.Explicit)]
public record struct PlayerState
{
    /// <summary>Current stamina of the player.</summary>
    [FieldOffset(0x00)]
    public float CurrentStamina;

    /// <summary>Current health of the player.</summary>
    [FieldOffset(0x04)]
    public float CurrentHealth;
    
    /// <summary>Current coin count of the player.</summary>
    [FieldOffset(0x0C)]
    public int CoinCount;
    
    /// <summary>Maximum health value, considering any upgrades.</summary>
    [FieldOffset(0x40)]
    public int MaxHealth;

    /// <summary>Maximum stamina value, considering any upgrades.</summary>
    [FieldOffset(0x44)]
    public int MaxStamina;
}