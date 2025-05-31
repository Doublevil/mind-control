# MindControl

MindControl is a .net hacking library for Windows that allows you to manipulate a game or any other process and its internal memory.

> **DO NOT use this library to cheat in online competitive games. Cheaters ruins the fun for everyone. If you ignore this warning, I will do my best to shut down your project.**

## Getting started

Here is a quick example to get you started.

```csharp
var myGame = ProcessMemory.OpenProcess("mygame").Result; // A process with this name must be running
var hpAddress = new PointerPath("mygame.exe+1D005A70,1C,8"); // See the docs for how to determine these

// Read values
var currentHp = myGame.Read<int>(hpAddress);
Console.WriteLine($"You have {currentHp}HP"); // Example output: "You have 50HP"

// Write values
myGame.Write(hpAddress, 9999);

// Find the first occurrence of a pattern in memory, with wildcard bytes
UIntPtr targetAddress = myGame.FindBytes("4D 79 ?? ?? ?? ?? ?? ?? 56 61 6C 75 65")
    .FirstOrDefault();

// ... And many more features
```

See [the documentation](doc/GetStarted.md) to get started, whether you already dabble in memory hacking or are completely new to it.

## Features

- Attach to any process easily by name or PID
- Address memory either through simple pointer addresses, or through dynamic pointer paths (e.g. `mygame.exe+1D005A70,1C,8`)
- Read and write byte arrays, booleans, numbers of any kind, strings and structures
- Inject DLLs to execute arbitrary code in the target process
- Search for byte sequences or patterns in the target process memory
- Manage memory allocations manually or automatically, to store data or code in the target process
- Insert assembly code with hooks
- Replace or remove existing code
- Start threads in the target process
- Designed for performance and simplicity of use
- Unit tested and made with care

## Comparison with other libraries

MindControl has a lot in common with other .net hacking libraries, the most popular one being [memory.dll](https://github.com/erfg12/memory.dll/). While the latter focuses on practicality and has some bias towards a specific use-case (game trainers), MindControl primarily aims to be more generic, reliable and maintainable, generally easier to understand and to use, and with similar or better performance.
