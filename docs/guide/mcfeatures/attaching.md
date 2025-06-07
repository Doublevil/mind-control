# Attaching to a process

Manipulating the memory of a running process with MindControl requires attaching to that process to get a `ProcessMemory` instance. In this section, we will cover how to attach to a process using the `MindControl` library.

## The Process Tracker

In most cases, the best way to attach to a process is to use the `ProcessTracker` class. This class has a very simple API: you specify the name of the process you want to attach to, and then you can get a `ProcessMemory` instance whenever you want to perform a memory manipulation operation. If the process is not running at the time of the call, it will return `null`.

Here is an example of how to use a `ProcessTracker` in a class:

```csharp
public class SlimeRancherProcess
{
    // Keep an instance of the ProcessTracker in your class and give it the name of the target process.
    private readonly ProcessTracker _processTracker = new("SlimeRancher");
    
    public int? GetCoinCount()
    {
        // Attempt to get the ProcessMemory instance for the target process
        // If the process is not running, this will return null
        var process = _processTracker.GetProcessMemory();
        if (process == null)
            return null;
        
        // Use the ProcessMemory instance to read a value from the target process
        var coinCountResult = process.Read<int>("UnityPlayer.dll+0168EEA0,8,100,28,20,80");
        return coinCountResult.ValueOr(0);
    }
}
```

The `ProcessTracker` class allows you to easily attach to your target process, without having to worry about:
- The order in which the processes are started: if your program starts first, `GetProcessMemory` will just return `null` until the target process is started.
- The process being closed: if the target process is closed, `GetProcessMemory` will return `null` after the process is closed.
- The process being restarted: if the target process is restarted, `GetProcessMemory` will return a new `ProcessMemory` instance for the new process. You don't have to care about this.

## Attaching directly through the `ProcessMemory` class

There are cases where you might want to attach to a process directly without using the `ProcessTracker`. For example:
- You know the process ID of the target process and want to attach to it directly.
- You are building a "one-shot" tool that attaches to a process, performs some operations, and then exits.
- You want to attach to multiple processes with the same name.

For these cases, you can use the `ProcessMemory` class directly. Here are examples of how to do this:

### Attaching to a process by name

```csharp
using MindControl;

var result = ProcessMemory.OpenProcessByName("MyTargetProcess"); // Replace with the actual process name
result.ThrowOnFailure(); // Throws an exception if the process could not be opened
var processMemory = result.Value;
```

### Attaching to a process by PID (process ID)

```csharp
using MindControl;

var result = ProcessMemory.OpenProcessById(1234); // Replace with the actual process ID
result.ThrowOnFailure(); // Throws an exception if the process could not be opened
var processMemory = result.Value;
```

### Attaching to a process with a System.Diagnostics.Process instance

```csharp
using MindControl;

var process = System.Diagnostics.Process.GetProcessById(1234); // You can use other methods to get the Process instance
var result = ProcessMemory.OpenProcess(process);
result.ThrowOnFailure(); // Throws an exception if the process could not be opened
var processMemory = result.Value;
```