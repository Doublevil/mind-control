# Using memory streams

For various use cases, you may need to read or write memory using streams. For these situations, MindControl provides a `ProcessMemoryStream` class, instantiated through a `ProcessMemory` instance.

This can be useful to read or write large amounts of data in a more efficient way, or more generally to work with memory as if it were a file.

```csharp
// Get a stream that starts at a specific address in the target process
ProcessMemoryStream stream = processMemory.GetMemoryStream(0x123456789);

// Read from the stream
byte[] buffer = new byte[1024];
int bytesRead = stream.Read(buffer, 0, buffer.Length);

// Write to the stream
stream.Write(buffer, 0, bytesRead);

// Seek to a specific position in the stream (here, go 8 bytes after the initial address 0x123456789)
stream.Seek(8, SeekOrigin.Begin);

// Dispose the stream when done
stream.Dispose();
```

> [!NOTE]
> Internally, the stream uses `ReadPartial` so that it will still read as much as possible upon reaching an unreadable section of memory. When failing to read a single byte, the `Read` method will return 0.