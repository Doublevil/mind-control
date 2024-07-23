using MindControl.Results;

namespace MindControl.Anchors;

/// <summary>Event arguments used when a freeze operation fails.</summary>
/// <param name="Failure">Failure that occurred when trying to freeze the value.</param>
/// <typeparam name="TFailure">Type of the failure.</typeparam>
public class FreezeFailureEventArgs<TFailure>(TFailure Failure) : EventArgs;

/// <summary>
/// Attaches to a <see cref="IValueAnchor{TValue,TReadFailure,TWriteFailure}"/> to prevent a memory area from changing
/// value.
/// </summary>
/// <typeparam name="TValue">Type of the value to freeze.</typeparam>
/// <typeparam name="TReadFailure">Type of the failure that can occur when reading the value.</typeparam>
/// <typeparam name="TWriteFailure">Type of the failure that can occur when writing the value.</typeparam>
public interface IValueFreezer<TValue, TReadFailure, TWriteFailure> : IDisposable
{
    /// <summary>Event raised when a freeze operation fails.</summary>
    event EventHandler<FreezeFailureEventArgs<ValueAnchorFailure>> FreezeFailed;
    
    /// <summary>Gets a boolean value indicating if a value is currently being frozen.</summary>
    bool IsFreezing { get; }
    
    /// <summary>Freezes the memory area that the anchor is attached to, preventing its value from changing, until
    /// either this instance is disposed, or <see cref="Unfreeze"/> is called.</summary>
    /// <param name="anchor">Anchor holding the memory value to freeze.</param>
    /// <returns>A result indicating success or failure.</returns>
    Result<string> StartFreezing(IValueAnchor<TValue, TReadFailure, TWriteFailure> anchor);
    
    /// <summary>Interrupts freezing if <see cref="StartFreezing"/> had been previously called.</summary>
    void Unfreeze();
}