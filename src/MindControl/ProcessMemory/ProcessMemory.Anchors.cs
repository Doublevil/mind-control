using MindControl.Anchors;

namespace MindControl;

// This partial class implements methods related to anchors
public partial class ProcessMemory
{
    /// <summary>
    /// Builds and returns an anchor for a value of type <typeparamref name="T"/> at the specified address. Anchors
    /// allow you to track and manipulate a specific value in memory. When not needed anymore, anchors should be
    /// disposed.
    /// </summary>
    /// <param name="address">Address of the value in memory.</param>
    /// <typeparam name="T">Type of the value to read and write.</typeparam>
    /// <returns>An anchor for the value at the specified address.</returns>
    public ValueAnchor<T> GetAnchor<T>(UIntPtr address)
        where T : struct
    {
        var memoryAdapter = new GenericMemoryAdapter<T>(new LiteralAddressResolver(address));
        return new ValueAnchor<T>(memoryAdapter, this);
    }

    /// <summary>
    /// Builds and returns an anchor for a value of type <typeparamref name="T"/> at the address referred by the given
    /// pointer path. Anchors allow you to track and manipulate a specific value in memory. When not needed anymore,
    /// anchors should be disposed.
    /// </summary>
    /// <param name="pointerPath">Pointer path to the address of the value in memory.</param>
    /// <typeparam name="T">Type of the value to read and write.</typeparam>
    /// <returns>An anchor for the value at the specified address.</returns>
    public ValueAnchor<T> GetAnchor<T>(PointerPath pointerPath)
        where T : struct
    {
        var memoryAdapter = new GenericMemoryAdapter<T>(new PointerPathResolver(pointerPath));
        return new ValueAnchor<T>(memoryAdapter, this);
    }

    /// <summary>
    /// Builds and returns an anchor for a byte array at the specified address. Anchors allow you to track and
    /// manipulate a specific value in memory. When not needed anymore, anchors should be disposed.
    /// </summary>
    /// <param name="address">Address of target byte array in memory.</param>
    /// <param name="size">Size of the target byte array.</param>
    /// <returns>An anchor for the array at the specified address.</returns>
    public ValueAnchor<byte[]> GetByteArrayAnchor(UIntPtr address, int size)
    {
        var memoryAdapter = new ByteArrayMemoryAdapter(new LiteralAddressResolver(address), size);
        return new ValueAnchor<byte[]>(memoryAdapter, this);
    }
    
    /// <summary>
    /// Builds and returns an anchor for a byte array at the address referred by the given pointer path. Anchors allow
    /// you to track and manipulate a specific value in memory. When not needed anymore, anchors should be disposed.
    /// </summary>
    /// <param name="pointerPath">Pointer path to the address of the target array in memory.</param>
    /// <param name="size">Size of the target byte array.</param>
    /// <returns>An anchor for the array at the specified address.</returns>
    public ValueAnchor<byte[]> GetByteArrayAnchor(PointerPath pointerPath, int size)
    {
        var memoryAdapter = new ByteArrayMemoryAdapter(new PointerPathResolver(pointerPath), size);
        return new ValueAnchor<byte[]>(memoryAdapter, this);
    }
    
    /// <summary>
    /// Builds and returns an anchor for a string pointer at the specified address. Anchors allow you to track and
    /// manipulate a specific value in memory. When not needed anymore, anchors should be disposed.
    /// String anchors are read-only. To write strings, please see the documentation.
    /// </summary>
    /// <param name="address">Address of the string pointer in memory.</param>
    /// <param name="stringSettings">Settings to read the string.</param>
    /// <returns>An anchor for the value at the specified address.</returns>
    public ValueAnchor<string> GetStringPointerAnchor(UIntPtr address, StringSettings stringSettings)
    {
        var memoryAdapter = new StringPointerMemoryAdapter(new LiteralAddressResolver(address), stringSettings);
        return new ValueAnchor<string>(memoryAdapter, this);
    }
    
    /// <summary>
    /// Builds and returns an anchor for a string pointer at the address referred by the specified pointer path. Anchors
    /// allow you to track and manipulate a specific value in memory. When not needed anymore, anchors should be
    /// disposed. String anchors are read-only. To write strings, please see the documentation.
    /// </summary>
    /// <param name="pointerPath">Pointer path to the address of the string pointer in memory.</param>
    /// <param name="stringSettings">Settings to read the string.</param>
    /// <returns>An anchor for the value at the specified address.</returns>
    public ValueAnchor<string> GetStringPointerAnchor(PointerPath pointerPath, StringSettings stringSettings)
    {
        var memoryAdapter = new StringPointerMemoryAdapter(new PointerPathResolver(pointerPath), stringSettings);
        return new ValueAnchor<string>(memoryAdapter, this);
    }
}