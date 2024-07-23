using MindControl.Results;

namespace MindControl.Anchors;

/// <summary>Provides methods to manipulate and track a specific value in memory.</summary>
/// <param name="memoryAdapter">Adapter that reads and writes the value from and to memory.</param>
/// <param name="processMemory">Instance of <see cref="ProcessMemory"/> attached to the target process.</param>
/// <typeparam name="TValue">Type of the value to read and write.</typeparam>
/// <typeparam name="TReadFailure">Type of the failure that can occur when reading the value.</typeparam>
/// <typeparam name="TWriteFailure">Type of the failure that can occur when writing the value.</typeparam>
public class ValueAnchor<TValue, TReadFailure, TWriteFailure>
    (IMemoryAdapter<TValue, TReadFailure, TWriteFailure> memoryAdapter, ProcessMemory processMemory)
    : IValueAnchor<TValue, TReadFailure, TWriteFailure>
{
    private bool _isDisposed;
    private IValueFreezer<TValue, TReadFailure, TWriteFailure>? _valueFreezer;
    
    /// <summary>Gets a boolean value indicating if a value is currently being frozen.</summary>
    public bool IsFrozen => _valueFreezer?.IsFreezing == true;
    
    /// <summary>Reads the value in the memory of the target process.</summary>
    /// <returns>A result holding either the value read from memory, or a failure.</returns>
    public Result<TValue, ValueAnchorFailure> Read()
    {
        if (_isDisposed)
            return new ValueAnchorFailureOnDisposedInstance();
        
        var result = memoryAdapter.Read(processMemory);
        if (result.IsFailure)
            return new ValueAnchorFailure<TReadFailure>(result.Error);
        return result.Value;
    }

    /// <summary>Writes the value to the memory of the target process.</summary>
    /// <param name="value">Value to write to memory.</param>
    /// <returns>A result indicating success or failure.</returns>
    public Result<ValueAnchorFailure> Write(TValue value)
    {
        if (_isDisposed)
            return new ValueAnchorFailureOnDisposedInstance();
        
        var result = memoryAdapter.Write(processMemory, value);
        if (result.IsFailure)
            return new ValueAnchorFailure<TWriteFailure>(result.Error);
        return Result<ValueAnchorFailure>.Success;
    }

    /// <summary>Freezes the memory area that the anchor is attached to, preventing its value from changing, until
    /// either this instance is disposed, or <see cref="Unfreeze"/> is called.</summary>
    public Result<string> Freeze(TValue value)
    {
        if (_isDisposed)
            return "This instance has been disposed.";
        
        if (_valueFreezer?.IsFreezing == true)
            _valueFreezer.Dispose();
        
        _valueFreezer = new TimerValueFreezer<TValue, TReadFailure, TWriteFailure>(value,
            TimeSpan.FromSeconds(1 / 145f));
        var result = _valueFreezer.StartFreezing(this);
        if (result.IsFailure)
            return result.Error;
        
        return Result<string>.Success;
    }
    
    /// <summary>Interrupts freezing if <see cref="Freeze"/> had been previously called.</summary>
    public void Unfreeze() => _valueFreezer?.Unfreeze();

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
    /// resources.</summary>
    public void Dispose()
    {
        if (_isDisposed)
            return;
        
        _isDisposed = true;
        _valueFreezer?.Dispose();
        processMemory.RemoveAnchor(this);
    }
}