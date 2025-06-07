# Freezing values in memory

This section covers a technique often called "freezing". The idea is to lock the value of a variable in memory so that it does not change, even as the game tries to modify it. This can be useful for debugging or, let's be real, to make cheats in games.

In MindControl, this is done through memory anchors. An anchor is like a persisting reference to a specific variable in memory, of a specific type, and it allows you to perform various operations on that specific target.

```csharp
var anchor = processMemory.GetAnchor<int>();

// Freeze that variable to the value 1234567
var freezer = anchor.Freeze(1234567);

// To unfreeze the variable, dispose the freezer
freezer.Dispose();
```

When you freeze a variable, MindControl will continuously write the specified value to the target memory location, effectively locking it in place. This means that even if the game changes the value, it will be very quickly overwritten with the frozen value you chose.

> [!NOTE]
> Freezing is resource-intensive, as it requires continuous writes to memory. Be careful not to overuse it, and make sure you dispose freezers that you no longer need.
