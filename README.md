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
```

See [the documentation](doc/GetStarted.md) for more information.

## Features

- Attach to any process easily by name or PID
- Address memory either through simple pointer addresses, or through dynamic pointer paths (e.g. `mygame.exe+1D005A70,1C,8`)
- Read a memory address as a byte array, boolean, or any basic number types
- Read a memory address as a string as simply as possible, or as complex as you need
- Write byte arrays, booleans and basic number types at any memory address
- Designed for performance and simplicity of use
- Unit tested and made with care

## Planned Features

MindControl is a work-in-progress. Here are some planned features:
- AoB scans with masking support
- Generic ValueTracker class that focuses on a single pointer path, with read and write operations, freeze and auto-refresh options
