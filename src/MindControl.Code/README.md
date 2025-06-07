# MindControl.Code

MindControl is a .net hacking library for Windows that allows you to manipulate a game or any other process and its internal memory.

This library is an extension of the main [MindControl](https://www.nuget.org/packages/MindControl) package, adding features related to code manipulation.

> **DO NOT use this library to cheat in online competitive games. Cheaters ruins the fun for everyone. If you ignore this warning, I will do my best to shut down your project.**

## Getting started

Here is a quick example to get you started.

```csharp
var myGame = ProcessMemory.OpenProcess("mygame").Result; // A process with this name must be running

// Write some assembly code, for example with Iced.Intel (a byte[] also works)
var assembler = new Assembler(64);
assembler.mov(rcx, value);
// ...

// Insert the code at the address "mygame.exe+0168EEA0"
CodeChange codeInjection = processMemory.InsertCodeAt("mygame.exe+0168EEA0", assembler).Value;
// Original code is untouched, your code will be executed right before the instruction at that address (through a hook).

// Disposing the CodeChange object restores the original code
codeInjection.Dispose();

// Check out the docs for more features!
```

See [the documentation](https://doublevil.github.io/mind-control/guide/introduction.html) to get started, whether you already dabble in memory hacking or are completely new to it.