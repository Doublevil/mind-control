namespace MindControl;

/// <summary>
/// Represents a reservation of a range of memory within a <see cref="MemoryAllocation"/>.
/// Reservations within an allocation cannot overlap and thus can be used to safely manage data or code storage over
/// a process.
/// Disposing a reservation will free the memory range for other uses.
/// </summary>
public class MemoryReservation
{
    /// <summary>
    /// Gets the memory range of this reservation.
    /// </summary>
    public MemoryRange Range { get; }

    /// <summary>
    /// Gets the starting address of the reservation.
    /// </summary>
    public UIntPtr Address => Range.Start;
    
    /// <summary>
    /// Gets the allocation that handles this reservation.
    /// </summary>
    public MemoryAllocation ParentAllocation { get; }
    
    /// <summary>
    /// Gets a boolean indicating if the reservation has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Gets the size of the reservation in bytes. This is a shortcut for calling <see cref="MemoryRange.GetSize"/> on
    /// the <see cref="Range"/>.
    /// </summary>
    public ulong Size => Range.GetSize();

    /// <summary>
    /// Builds a new <see cref="MemoryReservation"/> instance.
    /// </summary>
    /// <param name="range">Memory range of the reservation.</param>
    /// <param name="parentAllocation">Allocation that handles this reservation.</param>
    internal MemoryReservation(MemoryRange range, MemoryAllocation parentAllocation)
    {
        Range = range;
        ParentAllocation = parentAllocation;
    }

    /// <summary>
    /// Releases this reservation, allowing the parent <see cref="MemoryAllocation"/> to reuse the space for other data.
    /// </summary>
    public void Dispose()
    {
        if (IsDisposed)
            return;

        IsDisposed = true;
        ParentAllocation.FreeReservation(this);
    }
}