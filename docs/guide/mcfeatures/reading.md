# Reading memory

This section will explain how to read data from the target process using MindControl.

> [!NOTE]
> Reading string values is a more complex topic and is covered in the [Manipulating string](./strings.md) section of this guide.

## Reading numeric values

To read numeric values from the target process, you can use the `Read<T>` method of the `ProcessMemory` class. This method takes either an address or a pointer path, and returns a result containing either the read value in the asked type, or a failure in case the read operation failed.

Most numeric types are supported, including `int`, `float`, `double`, `long`, `bool`, and others. The read operation will attempt to read the value from the specified address or pointer path in the target process's memory.

Here are some examples of how to read numeric values from the target process:

### Reading an integer value from a pointer path

```csharp
var processMemory = MindControl.ProcessMemory.OpenProcessByName("MyTargetProcess").Value;
// Read an integer value from the specified address. Default to 0 if the read fails.
int health = processMemory.Read<int>("GameAssembly.dll+12345678").ValueOr(0);
```

### Reading a float value from an address

```csharp
var processMemory = MindControl.ProcessMemory.OpenProcessByName("MyTargetProcess").Value;
// Read a float value from the specified address. Default to 0.0f if the read fails.
float speed = processMemory.Read<float>(0x1A2B3C4).ValueOr(0.0f);
```

## Reading arbitrary structures

When you need to read multiple values in the same structure, you can define a struct that represents the data structure you want to read, and then use the same `Read<T>` method to read the entire structure at once. This is more performant than reading each field individually, especially when using pointer paths.

In most cases, you won't need all the fields of the structure, so you can define only the fields you are interested in. To handle these cases, you can use `[FieldOffset]` attributes to specify the offset of each field in the structure. This allows you to define a structure that only contains the fields you need, while still being able to read the entire structure in a single read operation.

Here are some examples:

### Reading a custom unmarked structure

```csharp
// Define a structure that represents the data you want to read
// Fields must be in the same order as they are in memory
struct PlayerStats
{
    public int Health;
    public float Speed;
    public long Score;
}

var processMemory = MindControl.ProcessMemory.OpenProcessByName("MyTargetProcess").Value;
PlayerStats playerStats = processMemory.Read<PlayerStats>("GameAssembly.dll+12345678").ValueOrDefault();
```

### Reading a custom structure with field offsets

```csharp
using System.Runtime.InteropServices;
// Define a structure with explicit field offsets
// This allows you to read only the fields you are interested in, even if they are not contiguous in memory.
[StructLayout(LayoutKind.Explicit)] // This is required for field offsets to be respected
struct PlayerStats
{
    [FieldOffset(0x00)] public int Health;
    [FieldOffset(0x0A)] public float Speed;
    [FieldOffset(0xF0)] public long Score;
}

var processMemory = MindControl.ProcessMemory.OpenProcessByName("MyTargetProcess").Value;
PlayerStats playerStats = processMemory.Read<PlayerStats>("GameAssembly.dll+12345678").ValueOrDefault();
// Remarks: even though only 3 fields are defined, the structure is 0xF8 bytes long, because it covers the whole memory
// area from 0x00 to the highest field offset plus its length. Don't use this approach if your fields are too far apart.
```

## Reading raw bytes

Sometimes, you may want to read raw bytes, without interpreting them as a specific type. You can use the `ReadBytes` method of the `ProcessMemory` class to read a specified number of bytes from a given address or pointer path.

```csharp
// Read 16 bytes from the specified address in the target process
byte[] rawData = processMemory.ReadBytes("GameAssembly.dll+12345678", 16).ValueOr([]);
```

There is also a `ReadBytesPartial` method variant that takes a `byte[]` array as a parameter, and populates it with the read data. Contrary to `ReadBytes`, this method will not fail if only some bytes could be read, and will return the number of bytes that were actually read.

```csharp
// Read up to 2048 bytes into a buffer, and get the number of bytes actually read
byte[] buffer = new byte[2048];
int bytesRead = processMemory.ReadBytesPartial("GameAssembly.dll+12345678", buffer, 2048).ValueOr(0);
```

> [!NOTE]
> `ReadBytesPartial` is only useful in particular cases, when reading large batches of an unreliable memory area. Most of the time, `ReadBytes` is simpler to use and preferable.
