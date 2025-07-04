﻿namespace MindControl.Native;

/// <summary>
/// Contains properties about a uniform range in the virtual address space of a process.
/// </summary>
public struct MemoryRangeMetadata
{
    /// <summary>
    /// Gets a pointer to the start address of the range.
    /// </summary>
    public UIntPtr StartAddress { get; init; }
    
    /// <summary>
    /// Gets the size of the range, in bytes.
    /// </summary>
    public UIntPtr Size { get; init; }
    
    /// <summary>
    /// Gets a boolean indicating if the memory is committed.
    /// </summary>
    public bool IsCommitted { get; init; }
    
    /// <summary>
    /// Gets a boolean indicating if the memory is free.
    /// </summary>
    public bool IsFree { get; init; }
    
    /// <summary>
    /// Gets a boolean indicating if the memory is guarded or marked for no access.
    /// </summary>
    public bool IsProtected { get; init; }
    
    /// <summary>
    /// Gets a boolean indicating if the memory is readable.
    /// </summary>
    public bool IsReadable { get; init; }
    
    /// <summary>
    /// Gets a boolean indicating if the memory is writable.
    /// </summary>
    public bool IsWritable { get; init; }
    
    /// <summary>
    /// Gets a boolean indicating if the memory is executable.
    /// </summary>
    public bool IsExecutable { get; init; }
    
    /// <summary>
    /// Gets a boolean indicating if the memory is mapped to a file.
    /// </summary>
    public bool IsMapped { get; init; }

    /// <summary>Returns the fully qualified type name of this instance.</summary>
    /// <returns>The fully qualified type name.</returns>
    public override string ToString()
    {
        return $"[{StartAddress:X}-{StartAddress + Size - 1:X}] ({(IsCommitted ? "C" : "-")}{(IsFree ? "F" : "-")}{(IsProtected ? "P" : "-")}{(IsReadable ? "R" : "-")}{(IsWritable ? "W" : "-")}{(IsExecutable ? "E" : "-")}{(IsMapped ? "M" : "-")})";
    }
}