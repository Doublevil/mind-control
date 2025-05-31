# Writing to memory

This section will guide you through writing to memory in a target process using `MindControl`.

> [!NOTE]
> Writing string values is a more complex topic and is covered in the [Manipulating string](./strings.md) section of this guide.

## Writing numeric values

To write numeric values to the target process, you can use the `Write` method of the `ProcessMemory` class. This method takes either an address or a pointer path, and the value to write, and returns a result indicating whether the write operation was successful.

Here are some examples of how to write numeric values to the target process:

### Writing an integer value to a pointer path

```csharp
var processMemory = MindControl.ProcessMemory.OpenProcessByName("MyTargetProcess").Value;
bool success = processMemory.Write("GameAssembly.dll+12345678", 100).IsSuccess;
```

### Writing a float value to an address

```csharp
var processMemory = MindControl.ProcessMemory.OpenProcessByName("MyTargetProcess").Value;
bool success = processMemory.Write(0x1A2B3C4, 3.14f).IsSuccess;
```

## Writing arbitrary structures

Just like when reading, you can define a struct that represents the data structure you want to write, and then use the `Write<T>` method to write the entire structure at once. This is more efficient than writing each field individually.

Here are some examples:

### Writing a custom unmarked structure

```csharp
// Define a structure that represents the data you want to write
// Fields must be in the same order as they are in memory
struct PlayerStats
{
    public int Health;
    public float Speed;
    public long Score;
}
var processMemory = MindControl.ProcessMemory.OpenProcessByName("MyTargetProcess").Value;
bool success = processMemory.Write("GameAssembly.dll+12345678", new PlayerStats { Health = 100, Speed = 5.0f, Score = 1000 }).IsSuccess;
```

### Writing a custom structure with field offsets

```csharp
using System.Runtime.InteropServices;
// See the previous section for more details on how to define structures with field offsets.
[StructLayout(LayoutKind.Explicit)]
struct PlayerStats
{
    [FieldOffset(0x00)] public int Health;
    [FieldOffset(0x0A)] public float Speed;
    [FieldOffset(0xF0)] public long Score;
}
var processMemory = MindControl.ProcessMemory.OpenProcessByName("MyTargetProcess").Value;
bool success = processMemory.Write("GameAssembly.dll+12345678", new PlayerStats { Health = 100, Speed = 5.0f, Score = 1000 }).IsSuccess;
```

## Writing raw bytes

If you need to write raw bytes to a specific address or pointer path, you can use the `WriteBytes` method. This method takes a byte array and writes it to the specified location in the target process.

```csharp
byte[] dataToWrite = new byte[] { 0x90, 0x90, 0x90 };
bool success = processMemory.WriteBytes("GameAssembly.dll+12345678", dataToWrite).IsSuccess;
```

## Memory protection strategies

All writing methods in `MindControl` have an additional parameter that allows you to specify how to handle memory protection. There are three options:
- `MemoryProtectionStrategy.Ignore`: No protection removal. The write will fail if the memory is protected, but if you know it isn't, this is the most performant option.
- `MemoryProtectionStrategy.Remove`: Removes the memory protection before writing. Memory protection will not be restored after the write operation, which may cause issues in some cases, but may be more performant than restoring it.
- `MemoryProtectionStrategy.RemoveAndRestore`: Temporarily removes the memory protection to allow writing, then restores it. This is the safest, but least performant option.

The default strategy is `MemoryProtectionStrategy.RemoveAndRestore`, for best compatibility. You can change this by passing a different strategy to the `Write` method.

```csharp
bool success = processMemory.Write("GameAssembly.dll+12345678", 100, MemoryProtectionStrategy.Ignore).IsSuccess;
```