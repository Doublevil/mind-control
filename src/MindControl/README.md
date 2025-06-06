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

See [the documentation](https://doublevil.github.io/mind-control/guide/introduction.html) to get started, whether you already dabble in memory hacking or are completely new to it.