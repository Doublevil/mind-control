using MindControl.Anchors;
using MindControl.Results;

namespace MindControl;

// This partial class implements methods related to anchors
public partial class ProcessMemory
{
    private readonly List<IValueAnchor> _anchors = new();
    
    /// <summary>
    /// Builds and returns an anchor for a value of type <typeparamref name="T"/> at the specified address. Anchors
    /// allow you to track and manipulate a specific value in memory. When not needed anymore, anchors should be
    /// disposed.
    /// </summary>
    /// <param name="address">Address of the value in memory.</param>
    /// <typeparam name="T">Type of the value to read and write.</typeparam>
    /// <returns>An anchor for the value at the specified address.</returns>
    public ValueAnchor<T, ReadFailure, WriteFailure> RegisterAnchor<T>(UIntPtr address)
        where T : struct
    {
        var memoryAdapter = new GenericMemoryAdapter<T, string>(new LiteralAddressResolver(address));
        var anchor = new ValueAnchor<T, ReadFailure, WriteFailure>(memoryAdapter, this);
        _anchors.Add(anchor);
        return anchor;
    }

    /// <summary>
    /// Builds and returns an anchor for a value of type <typeparamref name="T"/> at the address referred by the given
    /// pointer path. Anchors allow you to track and manipulate a specific value in memory. When not needed anymore,
    /// anchors should be disposed.
    /// </summary>
    /// <param name="pointerPath">Pointer path to the address of the value in memory.</param>
    /// <typeparam name="T">Type of the value to read and write.</typeparam>
    /// <returns>An anchor for the value at the specified address.</returns>
    public ValueAnchor<T, ReadFailure, WriteFailure> RegisterAnchor<T>(PointerPath pointerPath)
        where T : struct
    {
        var memoryAdapter = new GenericMemoryAdapter<T, PathEvaluationFailure>(new PointerPathResolver(pointerPath));
        var anchor = new ValueAnchor<T, ReadFailure, WriteFailure>(memoryAdapter, this);
        _anchors.Add(anchor);
        return anchor;
    }

    /// <summary>
    /// Builds and returns an anchor for a byte array at the specified address. Anchors allow you to track and
    /// manipulate a specific value in memory. When not needed anymore, anchors should be disposed.
    /// </summary>
    /// <param name="address">Address of target byte array in memory.</param>
    /// <param name="size">Size of the target byte array.</param>
    /// <returns>An anchor for the array at the specified address.</returns>
    public ValueAnchor<byte[], ReadFailure, WriteFailure> RegisterByteArrayAnchor(UIntPtr address, int size)
    {
        var memoryAdapter = new ByteArrayMemoryAdapter<string>(new LiteralAddressResolver(address), size);
        var anchor = new ValueAnchor<byte[], ReadFailure, WriteFailure>(memoryAdapter, this);
        _anchors.Add(anchor);
        return anchor;
    }
    
    /// <summary>
    /// Builds and returns an anchor for a byte array at the address referred by the given pointer path. Anchors allow
    /// you to track and manipulate a specific value in memory. When not needed anymore, anchors should be disposed.
    /// </summary>
    /// <param name="pointerPath">Pointer path to the address of the target array in memory.</param>
    /// <param name="size">Size of the target byte array.</param>
    /// <returns>An anchor for the array at the specified address.</returns>
    public ValueAnchor<byte[], ReadFailure, WriteFailure> RegisterByteArrayAnchor(PointerPath pointerPath, int size)
    {
        var memoryAdapter = new ByteArrayMemoryAdapter<PathEvaluationFailure>(
            new PointerPathResolver(pointerPath), size);
        var anchor = new ValueAnchor<byte[], ReadFailure, WriteFailure>(memoryAdapter, this);
        _anchors.Add(anchor);
        return anchor;
    }
    
    /// <summary>
    /// Builds and returns an anchor for a string pointer at the specified address. Anchors allow you to track and
    /// manipulate a specific value in memory. When not needed anymore, anchors should be disposed.
    /// String anchors are read-only. To write strings, please see the documentation.
    /// </summary>
    /// <param name="address">Address of the string pointer in memory.</param>
    /// <param name="stringSettings">Settings to read the string.</param>
    /// <returns>An anchor for the value at the specified address.</returns>
    public ValueAnchor<string, StringReadFailure, NotSupportedFailure> RegisterStringPointerAnchor(UIntPtr address,
        StringSettings stringSettings)
    {
        var memoryAdapter = new StringPointerMemoryAdapter<string>(new LiteralAddressResolver(address), stringSettings);
        var anchor = new ValueAnchor<string, StringReadFailure, NotSupportedFailure>(memoryAdapter, this);
        _anchors.Add(anchor);
        return anchor;
    }
    
    /// <summary>
    /// Builds and returns an anchor for a string pointer at the address referred by the specified pointer path. Anchors
    /// allow you to track and manipulate a specific value in memory. When not needed anymore, anchors should be
    /// disposed. String anchors are read-only. To write strings, please see the documentation.
    /// </summary>
    /// <param name="pointerPath">Pointer path to the address of the string pointer in memory.</param>
    /// <param name="stringSettings">Settings to read the string.</param>
    /// <returns>An anchor for the value at the specified address.</returns>
    public ValueAnchor<string, StringReadFailure, NotSupportedFailure> RegisterStringPointerAnchor(
        PointerPath pointerPath, StringSettings stringSettings)
    {
        var memoryAdapter = new StringPointerMemoryAdapter<PathEvaluationFailure>(
            new PointerPathResolver(pointerPath), stringSettings);
        var anchor = new ValueAnchor<string, StringReadFailure, NotSupportedFailure>(memoryAdapter, this);
        _anchors.Add(anchor);
        return anchor;
    }
    
    /// <summary>Removes an anchor from the list of anchors. Designed to be called by the anchor on disposal.</summary>
    /// <param name="anchor">Anchor to remove from the list of anchors.</param>
    internal void RemoveAnchor(IValueAnchor anchor) => _anchors.Remove(anchor);
}