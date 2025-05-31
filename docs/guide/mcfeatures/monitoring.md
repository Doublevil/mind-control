# Monitoring value changes in memory

This section covers how to monitor changes to a variable in memory. This can be useful if you need your application to react to changes in your target process, without having to set up timers or other polling mechanisms yourself.

You can monitor changes to a variable in memory through anchors. An anchor is a persistent reference to a specific variable in memory, of a specific type, and it allows you to perform various operations on that specific target.

```csharp
var anchor = processMemory.GetAnchor<int>();
var watcher = anchor.Watch(TimeSpan.FromMilliseconds(100)); // Read every 100 milliseconds
watcher.ValueChanged += (_, args) =>
{
    Console.WriteLine($"Value changed from {args.PreviousValue} to {args.NewValue}.");
};
watcher.ValueLost += (_, args) =>
{
    Console.WriteLine($"Value lost. Last known value is {args.LastKnownValue}.");
};
watcher.ValueReacquired += (_, args) =>
{
    Console.WriteLine($"Value no longer lost. The new value is {args.NewValue}.");
};

// ...

// To stop watching the value, dispose the watcher
watcher.Dispose();
```
