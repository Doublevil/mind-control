# MindControl

MindControl is a .net hacking library for Windows that allows you to manipulate a game or any other process and its internal memory.

> **DO NOT use this library to cheat in online competitive games. Cheaters ruins the fun for everyone. If you ignore this warning, I will do my best to shut down your project.**

## Getting started

Here is a quick example to get you started.

```csharp
var myGame = ProcessMemory.OpenProcess("mygame.exe"); // A process with this name must be running
var hpAddress = new PointerPath("mygame.exe+1D005A70,1C,8"); // See the docs for how to determine these

// Read values
int currentHp = myGame.ReadInt(hpAddress);
Console.WriteLine($"You have {currentHp}HP"); // Example output: "You have 50HP"

// Write values
myGame.WriteInt(hpAddress, 9999);

// Find the first occurrence of a pattern in memory, with wildcard bytes
UIntPtr targetAddress = myGame.FindBytes("4D 79 ?? ?? ?? ?? ?? ?? 56 61 6C 75 65")
    .FirstOrDefault();
```

See [the documentation](doc/GetStarted.md) for more information.

## Features

- Attach to any process easily by name or PID
- Address memory either through simple pointer addresses, or through dynamic pointer paths (e.g. `mygame.exe+1D005A70,1C,8`)
- Read a memory address as a byte array, boolean, or any basic number types
- Read a memory address as a string as simply as possible, or as complex as you need
- Write byte arrays, booleans and basic number types at any memory address
- Inject DLLs to execute arbitrary code in the target process
- Search for byte patterns in the target process memory
- Designed for performance and simplicity of use
- Unit tested and made with care

## Comparison with other libraries

MindControl is a small library that focuses on the most common use cases for hacking.

It is not as feature-rich as the most used .net hacking library, [memory.dll](https://github.com/erfg12/memory.dll/), but it aims to be easier to use, have comparable performance, and most importantly to be more reliable and maintainable.

If you are considering MindControl but unsure if it has the features you need, here is a comparison table. 

| Feature                     | MindControl | memory.dll
|-----------------------------|--- |--- |
| **Handle pointer paths**    | ✔️ | ✔️ |
| **Read primitive types**    | ✔️ | ✔️ |
| **Write primitive types**   | ✔️ | ✔️ |
| **Read strings**            | ✔️ | ✔️ |
| **Write strings**           | ❌ | ✔️ |
| **Array of bytes scanning** | ✔️ | ✔️ |
| **Inject DLLs**             | ✔️ | ✔️ |
| **State watchers**          | ✔️ | ❌ |
| **Create code caves**       | ❌ | ✔️ |
| **Bind to UI elements**     | ❌ | ✔️ |
| **Freeze values**           | ❌ | ✔️ |
| **Set focus to process**    | ❌ | ✔️ |
| **Load from .ini file**     | ❌ | ✔️ |
| **Suspend process**         | ❌ | ✔️ |
| **Dump process memory**     | ❌ | ✔️ |
| **Manipulate threads**      | ❌ | ✔️ |
