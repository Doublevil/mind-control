using MindControl.Native;

namespace MindControl;

/// <summary>
/// A stream that reads or writes into the memory of a process.
/// </summary>
public class ProcessMemoryStream : Stream
{
    private readonly IOperatingSystemService _osService;
    private readonly IntPtr _processHandle;
    private readonly UIntPtr _baseAddress;
    private UIntPtr _position;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessMemoryStream"/> class.
    /// </summary>
    /// <param name="osService">Service that provides system-specific process memory read and write features.</param>
    /// <param name="processHandle">Handle of the target process.</param>
    /// <param name="baseAddress">Starting address of the memory range to read or write.</param>
    internal ProcessMemoryStream(IOperatingSystemService osService, IntPtr processHandle, UIntPtr baseAddress)
    {
        _osService = osService;
        _processHandle = processHandle;
        _baseAddress = baseAddress;
        _position = baseAddress;
    }

    /// <summary>Returns True to indicate that this stream supports reading.</summary>
    /// <returns><see langword="true" />.</returns>
    public override bool CanRead => true;

    /// <summary>Returns False to indicate that this stream does not support seeking.</summary>
    /// <returns><see langword="true" />.</returns>
    public override bool CanSeek => true;

    /// <summary>Returns True to indicate that this stream supports writing.</summary>
    /// <returns><see langword="true" /></returns>
    public override bool CanWrite => true;

    /// <summary>In this implementation, this getter is not supported.</summary>
    /// <exception cref="T:System.NotSupportedException">Always thrown, as this implementation does not support
    /// seeking.</exception>
    /// <returns>A long value representing the length of the stream in bytes.</returns>
    public override long Length => throw new NotSupportedException();

    /// <summary>Gets or sets the position within the current stream, relative to the start address.</summary>
    public override long Position
    {
        get => (long)(_position.ToUInt64() - _baseAddress.ToUInt64());
        set => _position = (UIntPtr)(_baseAddress.ToUInt64() + (ulong)value);
    }

    /// <summary>In this implementation, does not perform any action.</summary>
    public override void Flush() { }

    /// <summary>Reads a sequence of bytes from the current stream and advances the position within the stream by the
    /// number of bytes read.</summary>
    /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array
    /// with the values between <paramref name="offset" /> and (<paramref name="offset" /> + <paramref name="count" /> -
    /// 1) replaced by the bytes read from the current source.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin storing the data
    /// read from the current stream.</param>
    /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
    /// <exception cref="T:System.ArgumentException">The sum of <paramref name="offset" /> and <paramref name="count" />
    /// is larger than the buffer length.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="buffer" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is negative.</exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurs.</exception>
    /// <exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed.</exception>
    /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if
    /// that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
        if (buffer.Length - offset < count)
            throw new ArgumentException("The buffer is too small to store the requested number of bytes.",
                nameof(buffer));
        
        var result = _osService.ReadProcessMemoryPartial(_processHandle, _position, buffer, offset, (ulong)count);
        
        // If no byte was read, return 0.
        // We won't throw an exception here because multiple sorts of errors might mean that we cannot read any further
        // and thus it's safer to consider it as the end of the stream.
        if (result.IsFailure)
            return 0;

        ulong read = result.Value;
        _position = (UIntPtr)(_position.ToUInt64() + read);
        return (int)read;
    }

    /// <summary>Moves the position within the current stream by the specified offset, relative to either the start of
    /// the stream (the base address), or the current position. Seeking relative to the end is not supported by this
    /// implementation, as there is no end by design.</summary>
    /// <param name="offset">A byte offset relative to the <paramref name="origin" /> parameter.</param>
    /// <param name="origin">A value of type <see cref="T:System.IO.SeekOrigin" /> indicating the reference point used
    /// to obtain the new position. In this implementation, <see cref="SeekOrigin.End"/> is not supported.</param>
    /// <exception cref="T:System.NotSupportedException">Thrown when <paramref name="origin"/> is set to
    /// <see cref="SeekOrigin.End"/>, as this implementation has no length and thus no end.</exception>
    /// <returns>The new position within the current stream.</returns>
    public override long Seek(long offset, SeekOrigin origin)
    {
        _position = origin switch
        {
            SeekOrigin.Begin => (UIntPtr)(_baseAddress.ToUInt64() + (ulong)offset),
            SeekOrigin.Current => (UIntPtr)(_position.ToUInt64() + (ulong)offset),
            SeekOrigin.End => throw new NotSupportedException("Seeking relative to the end is not supported."),
            _ => throw new ArgumentOutOfRangeException(nameof(origin), "Invalid seek origin.")
        };

        return Position;
    }

    /// <summary>In this implementation, this method is not supported.</summary>
    /// <param name="value">The desired length of the current stream in bytes.</param>
    /// <exception cref="T:System.NotSupportedException">Always thrown, as this stream implementation does not support
    /// seeking.</exception>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>Writes a sequence of bytes to the current stream and advances the current position within this stream
    /// by the number of bytes written.</summary>
    /// <param name="buffer">An array of bytes. This method copies <paramref name="count" /> bytes from
    /// <paramref name="buffer" /> to the current stream.</param>
    /// <param name="offset">The zero-based byte offset in <paramref name="buffer" /> at which to begin copying bytes to
    /// the current stream.</param>
    /// <param name="count">The number of bytes to be written to the current stream.</param>
    /// <exception cref="T:System.ArgumentException">The sum of <paramref name="offset" /> and <paramref name="count" />
    /// is greater than the buffer length.</exception>
    /// <exception cref="T:System.ArgumentNullException">
    /// <paramref name="buffer" /> is <see langword="null" />.</exception>
    /// <exception cref="T:System.ArgumentOutOfRangeException">
    /// <paramref name="offset" /> or <paramref name="count" /> is negative.</exception>
    /// <exception cref="T:System.IO.IOException">An I/O error occurred.</exception>
    /// <exception cref="T:System.ObjectDisposedException">
    /// <see cref="M:System.IO.Stream.Write(System.Byte[],System.Int32,System.Int32)" /> was called after the stream was
    /// closed.</exception>
    public override void Write(byte[] buffer, int offset, int count)
    {
        if (offset < 0)
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative.");
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
        if (buffer.Length - offset < count)
            throw new ArgumentException("The buffer is too small to write the requested number of bytes.",
                nameof(buffer));

        var result = _osService.WriteProcessMemory(_processHandle, _position, buffer.AsSpan(offset, count));
        
        // Unlike Read, we will throw an exception if the write operation failed, because write operations are expected
        // to fully complete.
        if (result.IsFailure)
            throw new IOException("Failed to write into the process memory.", result.ToException());
        
        _position += (UIntPtr)count;
    }
}
            