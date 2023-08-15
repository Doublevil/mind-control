# MindControl

MindControl is a .net hacking library for Windows that allows you to manipulate a game or any other process and its internal memory.

> **DO NOT use this library to cheat in online competitive games. Cheaters ruins the fun for everyone. If you ignore this warning, I will do my best to shut down your project.**

## Getting started

Here is a quick example to get you started.

```csharp
var myGame = ProcessMemory.OpenProcess("mygame"); // A process with this name must be running
var hpAddress = new PointerPath("mygame.exe+1D005A70,1C,8"); // See the docs for how to determine these

// Read values
int currentHp = myGame.ReadInt(hpAddress);
Console.WriteLine($"You have {currentHp}HP"); // Example output: "You have 50HP"

// Write values
myGame.WriteInt(hpAddress, 9999);
```

See [the full documentation on GitHub](https://github.com/Doublevil/mind-control) for more info.