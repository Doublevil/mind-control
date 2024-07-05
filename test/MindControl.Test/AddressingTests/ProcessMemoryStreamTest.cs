using System.Runtime.InteropServices;
using MindControl.Test.ProcessMemoryTests;
using NUnit.Framework;

namespace MindControl.Test.AddressingTests;

/// <summary>
/// Tests the <see cref="ProcessMemoryStream"/> class.
/// Because this class is strongly bound to a ProcessMemory, we have to use the
/// <see cref="ProcessMemory.GetMemoryStream(UIntPtr)"/> method to create instances, so the tests below will use an
/// actual instance of <see cref="ProcessMemory"/> and depend on that method.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryStreamTest : BaseProcessMemoryTest
{
    /// <summary>
    /// Tests the <see cref="ProcessMemory.GetMemoryStream(UIntPtr)"/> method.
    /// This is not part of the tested class, but it is used to create instances of it, so that's why it's here.
    /// </summary>
    [Test]
    public void GetMemoryStreamTest()
    {
        using var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(stream, Is.Not.Null);
    }

    #region Getters and setters
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.CanRead"/> getter.
    /// Must always return true.
    /// </summary>
    [Test]
    public void CanReadTest()
    {
        using var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(stream.CanRead, Is.True);
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.CanSeek"/> getter.
    /// Must always return false.
    /// </summary>
    [Test]
    public void CanSeekTest()
    {
        using var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(stream.CanSeek, Is.False);
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.CanWrite"/> getter.
    /// Must always return true.
    /// </summary>
    [Test]
    public void CanWriteTest()
    {
        using var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(stream.CanWrite, Is.True);
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Length"/> getter.
    /// Must always throw, as the stream has no length by design.
    /// </summary>
    [Test]
    public void LengthTest()
    {
        var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(() => stream.Length, Throws.InstanceOf<NotSupportedException>());
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.SetLength"/> method.
    /// Must always throw, as the stream has no length by design.
    /// </summary>
    [Test]
    public void SetLengthTest()
    {
        var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(() => stream.SetLength(0), Throws.InstanceOf<NotSupportedException>());
        Assert.That(() => stream.SetLength(32), Throws.InstanceOf<NotSupportedException>());
        Assert.That(() => stream.SetLength(256), Throws.InstanceOf<NotSupportedException>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Position"/> getter.
    /// Must return 0 on a new instance, and advance as data is read or written.
    /// </summary>
    [Test]
    public void PositionTest()
    {
        using var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(stream.Position, Is.Zero);
        
        stream.ReadByte();
        Assert.That(stream.Position, Is.EqualTo(1));
        
        stream.WriteByte(0x00);
        Assert.That(stream.Position, Is.EqualTo(2));
        
        stream.Position = 0;
        Assert.That(stream.Position, Is.Zero);
    }

    #endregion
    
    #region Flush
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Flush"/> method.
    /// Must not throw, but is expected to do nothing, as the stream is not buffered.
    /// </summary>
    [Test]
    public void FlushTest()
    {
        var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(() => stream.Flush(), Throws.Nothing);
    }
    
    #endregion
    
    #region Seek
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Seek(long,SeekOrigin)"/> method.
    /// Tests that seeking back and forth with <see cref="SeekOrigin.Begin"/> changes the position as expected.
    /// </summary>
    [Test]
    public void SeekFromBeginTest()
    {
        using var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        stream.Seek(8, SeekOrigin.Begin);
        Assert.That(stream.Position, Is.EqualTo(8));
        
        stream.Seek(0, SeekOrigin.Begin);
        Assert.That(stream.Position, Is.Zero);
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Seek(long,SeekOrigin)"/> method.
    /// Tests that seeking with <see cref="SeekOrigin.Current"/> changes the position as expected.
    /// </summary>
    [Test]
    public void SeekFromCurrentTest()
    {
        using var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        stream.Seek(8, SeekOrigin.Current);
        Assert.That(stream.Position, Is.EqualTo(8));
        
        stream.Seek(0, SeekOrigin.Current);
        Assert.That(stream.Position, Is.EqualTo(8));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Seek(long,SeekOrigin)"/> method.
    /// Tests that seeking with <see cref="SeekOrigin.End"/> throws a <see cref="NotSupportedException"/>, as there is
    /// no end of the stream, by design.
    /// </summary>
    [Test]
    public void SeekFromEndTest()
    {
        var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(() => stream.Seek(8, SeekOrigin.End), Throws.InstanceOf<NotSupportedException>());
    }
    
    #endregion
    
    #region Read

    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Read(byte[],int,int)"/> method.
    /// Performs a single read operation over a known value.
    /// Must read the expected data from the process memory.
    /// </summary>
    [Test]
    public void SimpleReadTest()
    {
        using var stream = TestProcessMemory!.GetMemoryStream(GetPointerPathForValueAtIndex(IndexOfOutputULong)).Value;
        var buffer = new byte[8];
        int byteCount = stream.Read(buffer, 0, 8);
        ulong readValue = MemoryMarshal.Read<ulong>(buffer);
        
        Assert.That(byteCount, Is.EqualTo(8));
        Assert.That(readValue, Is.EqualTo(76354111324644L)); // Known value
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Read(byte[],int,int)"/> method.
    /// Performs two read operations over known values.
    /// Must read the expected data from the process memory.
    /// This tests that the read methods read from the current position in the stream, and not always from the start.
    /// </summary>
    [Test]
    public void MultipleReadTest()
    {
        using var stream = TestProcessMemory!.GetMemoryStream(GetPointerPathForValueAtIndex(IndexOfOutputLong)).Value;
        var buffer = new byte[8];
        int firstByteCount = stream.Read(buffer, 0, 8);
        long firstValue = MemoryMarshal.Read<long>(buffer);
        
        int secondByteCount = stream.Read(buffer, 0, 8);
        ulong secondValue = MemoryMarshal.Read<ulong>(buffer);
        
        Assert.That(firstByteCount, Is.EqualTo(8));
        Assert.That(firstValue, Is.EqualTo(-65746876815103L)); // Known value
        
        Assert.That(secondByteCount, Is.EqualTo(8));
        Assert.That(secondValue, Is.EqualTo(76354111324644L)); // Known value
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Read(byte[],int,int)"/> method.
    /// Performs a single read operation over a known value, with an offset.
    /// Must read the expected data from the process memory, with the offset applied.
    /// </summary>
    [Test]
    public void ReadWithOffsetTest()
    {
        using var stream = TestProcessMemory!.GetMemoryStream(GetPointerPathForValueAtIndex(IndexOfOutputULong)).Value;
        var buffer = new byte[12];
        int byteCount = stream.Read(buffer, 4, 8); // Use an offset of 4
        ulong readValue = MemoryMarshal.Read<ulong>(buffer.AsSpan(4)); // Read value from index 4
        
        Assert.That(byteCount, Is.EqualTo(8));
        Assert.That(buffer.Take(4), Is.All.Zero); // First 4 bytes are untouched
        Assert.That(readValue, Is.EqualTo(76354111324644L)); // Known value
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Read(byte[],int,int)"/> method.
    /// Calls the read method with an offset that goes beyond the capacity of the buffer.
    /// Must throw a <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    public void ReadWithImpossibleOffsetTest()
    {
        var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(() => stream.Read(new byte[8], 8, 1), Throws.InstanceOf<ArgumentException>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Read(byte[],int,int)"/> method.
    /// Calls the read method with a count that goes beyond the capacity of the buffer.
    /// Must throw a <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    public void ReadWithImpossibleCountTest()
    {
        var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(() => stream.Read(new byte[8], 0, 9), Throws.InstanceOf<ArgumentException>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Read(byte[],int,int)"/> method.
    /// Calls the read method with a combination of offset and count that go beyond the capacity of the buffer.
    /// Must throw a <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    public void ReadWithImpossibleOffsetAndCountTest()
    {
        var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(() => stream.Read(new byte[8], 3, 6), Throws.InstanceOf<ArgumentException>());
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Read(byte[],int,int)"/> method.
    /// Performs a read operation that starts in a readable region, but ends in a non-readable region.
    /// The read operation should succeed (not throw), and the bytes that are readable should be read.
    /// </summary>
    [Test]
    public void ReadOnTheEdgeOfValidMemoryTest()
    {
        // Prepare a segment of memory that is isolated from other memory regions, and has a known sequence of bytes
        // at the end.
        var bytesAtTheEnd = new byte[] { 0x1, 0x2, 0x3, 0x4 };
        var allocatedMemory = TestProcessMemory!.Allocate(0x1000, false).Value;
        var targetAddress = allocatedMemory.Range.End - 4;
        var writeResult = TestProcessMemory.WriteBytes(targetAddress, bytesAtTheEnd, MemoryProtectionStrategy.Ignore);
        Assert.That(writeResult.IsSuccess, Is.True);
        
        // Attempt to read 8 bytes from the target address, which is 4 bytes before the end of the isolated segment.
        using var stream = TestProcessMemory.GetMemoryStream(targetAddress);
        var buffer = new byte[8];
        int byteCount = stream.Read(buffer, 0, 8);
        
        // We should have read only 4 bytes, and the first 4 bytes of the buffer should be the bytes we wrote at the end
        // of our memory segment.
        Assert.That(byteCount, Is.EqualTo(4));
        Assert.That(buffer.Take(4), Is.EqualTo(bytesAtTheEnd));
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Read(byte[],int,int)"/> method.
    /// Performs a read operation that starts in an unreadable region.
    /// The read operation should succeed (not throw) but return that 0 bytes were read.
    /// </summary>
    [Test]
    public void ReadOnUnreadableMemoryTest()
    {
        using var stream = TestProcessMemory!.GetMemoryStream(UIntPtr.MaxValue);
        var buffer = new byte[8];
        int byteCount = stream.Read(buffer, 0, 8);
        
        Assert.That(byteCount, Is.Zero);
    }
    
    #endregion
    
    #region Write
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Write(byte[],int,int)"/> method.
    /// Performs a single write operation and read back that value using a process memory read method.
    /// Must read back the data that was written from the stream.
    /// </summary>
    [Test]
    public void SimpleWriteTest()
    {
        var address = OuterClassPointer + 0x28;
        using var stream = TestProcessMemory!.GetMemoryStream(address);
        var buffer = new byte[] { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7 };
        stream.Write(buffer, 0, 8);
        var readValue = TestProcessMemory.ReadBytes(address, 8).Value;
        
        Assert.That(readValue, Is.EqualTo(buffer));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Write(byte[],int,int)"/> method.
    /// Performs two write operations over known values, and read back the data using a process memory read method.
    /// Must read back the data that was written from the stream.
    /// This tests that the write methods write from the current position in the stream, and not always from the start.
    /// </summary>
    [Test]
    public void MultipleWriteTest()
    {
        var startAddress = OuterClassPointer + 0x20;
        using var stream = TestProcessMemory!.GetMemoryStream(startAddress);
        var buffer = new byte[] { 0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7 };
        stream.Write(buffer, 0, 8);
        stream.Write(buffer, 0, 8);
        var readValue = TestProcessMemory.ReadBytes(startAddress, 16).Value;
        
        Assert.That(readValue, Is.EqualTo(buffer.Concat(buffer)));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Write(byte[],int,int)"/> method.
    /// Performs a single write operation, with an offset, and read back that value using a process memory read method.
    /// Must read back only the portion of the data that starts at the offset in the written buffer.
    /// </summary>
    [Test]
    public void WriteWithOffsetTest()
    {
        var startAddress = OuterClassPointer + 0x28;
        using var stream = TestProcessMemory!.GetMemoryStream(startAddress);
        var buffer = new byte[]
        {
            0x0, 0x1, 0x2, 0x3, 0x4, 0x5, 0x6, 0x7,
            0x8, 0x9, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF
        };
        stream.Write(buffer, 8, 8); // Use an offset of 8, which should write from 0x8 to 0xF
        var readValue = TestProcessMemory.ReadBytes(startAddress, 8).Value;
        
        Assert.That(readValue, Is.EqualTo(new byte[] { 0x8, 0x9, 0xA, 0xB, 0xC, 0xD, 0xE, 0xF }));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Write(byte[],int,int)"/> method.
    /// Calls the read method with an offset that goes beyond the capacity of the buffer.
    /// Must throw a <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    public void WriteWithImpossibleOffsetTest()
    {
        var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(() => stream.Write(new byte[8], 8, 1), Throws.InstanceOf<ArgumentException>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Write(byte[],int,int)"/> method.
    /// Calls the read method with a count that goes beyond the capacity of the buffer.
    /// Must throw a <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    public void WriteWithImpossibleCountTest()
    {
        var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(() => stream.Write(new byte[8], 0, 9), Throws.InstanceOf<ArgumentException>());
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Write(byte[],int,int)"/> method.
    /// Calls the read method with a combination of offset and count that go beyond the capacity of the buffer.
    /// Must throw a <see cref="ArgumentException"/>.
    /// </summary>
    [Test]
    public void WriteWithImpossibleOffsetAndCountTest()
    {
        var stream = TestProcessMemory!.GetMemoryStream(OuterClassPointer);
        Assert.That(() => stream.Write(new byte[8], 3, 6), Throws.InstanceOf<ArgumentException>());
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Write(byte[],int,int)"/> method.
    /// Performs a write operation that starts in a writable region, but ends in a non-writable region.
    /// The write operation should fail with an <see cref="IOException"/>.
    /// </summary>
    [Test]
    public void WriteOnTheEdgeOfValidMemoryTest()
    {
        // Prepare a segment of memory that is isolated from other memory regions, and has a known sequence of bytes
        // at the end.
        var bytesAtTheEnd = new byte[] { 0x1, 0x2, 0x3, 0x4 };
        var allocatedMemory = TestProcessMemory!.Allocate(0x1000, false).Value;
        var targetAddress = allocatedMemory.Range.End - 4;
        var writeResult = TestProcessMemory.WriteBytes(targetAddress, bytesAtTheEnd, MemoryProtectionStrategy.Ignore);
        Assert.That(writeResult.IsSuccess, Is.True);

        // Attempt to write 8 bytes from the target address, which is 4 bytes before the end of the isolated segment.
        var stream = TestProcessMemory.GetMemoryStream(targetAddress);
        Assert.That(() => stream.Write(new byte[8], 0, 8), Throws.InstanceOf<IOException>());
    }

    /// <summary>
    /// Tests the <see cref="ProcessMemoryStream.Write(byte[],int,int)"/> method.
    /// Performs a write operation that starts in an unwritable region.
    /// The write operation should fail with an <see cref="IOException"/>.
    /// </summary>
    [Test]
    public void WriteOnUnreadableMemoryTest()
    {
        var stream = TestProcessMemory!.GetMemoryStream(UIntPtr.MaxValue);
        Assert.That(() => stream.Write(new byte[8], 0, 8), Throws.InstanceOf<IOException>());
    }
    
    #endregion
}

/// <summary>
/// Runs the tests from <see cref="ProcessMemoryStreamTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryStreamTestX86 : ProcessMemoryStreamTest
{
    /// <summary>Gets a boolean value defining which version of the target app is used.</summary>
    protected override bool Is64Bit => false;
}