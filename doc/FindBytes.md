# Finding patterns in memory with FindBytes

MindControl allows you to search for byte patterns in the memory of your target process. This can be useful to find the location of a value you want to hack, or to find a function you want to hook.

```csharp
var myGame = ProcessMemory.OpenProcess("mygame");
UIntPtr targetAddress = myGame.FindBytes("4D 79 ?? ?? ?? ?? ?? ?? 56 61 6C 75 65")
    .FirstOrDefault();
```

This is also called "AoB scanning" (where AoB stands for "Array of bytes").

## Pattern syntax

The pattern you provide to `FindBytes` is a string of hexadecimal bytes, optionally separated by spaces. Each byte can be a specific value (e.g. `4D`), or a wildcard (e.g. `??`). The wildcard will match any byte.

You can also use partial wildcards, for example `4?` or `?D` to match any byte that starts with `4` or ends with `D`.

For better performances, avoid starting your pattern with a wildcard.

## Search range

By default, `FindBytes` will search the entire memory of the target process. This can be slow, especially if the process has a lot of memory.

You can limit the search to a specific range of memory by using the `range` optional parameter. 

The example below demonstrates how to search only in the range of a specific module.

```csharp
var myGame = ProcessMemory.OpenProcess("mygame");

// Get the range for the module that contains the game executable
// Instead of "mygame.exe", you can use the name of any module loaded in the process (usually a DLL)
var myModuleRange = myGame.Modules.FirstOrDefault(m => m.Name == "mygame.exe");
List<UIntPtr> results = myGame.FindBytes("4D 79 ?? ?? 65", range: myModuleRange)
    .ToList();
```

You can also specify a custom range by creating a `MemoryRange` instance:

```csharp
 // Both of the following ranges can be used to search from 0x5000 to 0x5FFF included
var rangeWithStartAndEnd = new MemoryRange(0x5000, 0x5FFF);
var rangeWithStartAndSize = new MemoryRange(0x5000, 0x1000);
```

To optimize the performance of your search, you should always specify a range. Usually, you can figure out at least the module that contains the value you are looking for, but the more specific you can be, the faster the search will be.

## Search parameters

The `FindBytes` method takes an optional `FindBytesSetting` parameter that allows you to specify what kind of memory you want to search, and limit the number of results.

Depending on what kind of data you are looking for, you can specify constraints on the kind of memory you want to search. For example, you can search only executable memory, or only memory that is writable.

Here is an example of how to search for a pattern in the executable code of your target process:

```csharp
var searchSettings = new FindBytesSettings
{
    SearchExecutable = true,
    SearchWritable = false
};
List<UIntPtr> codeResults = myGame.FindBytes("4D 79 ?? ?? 65", settings: searchSettings)
    .ToList();
```

Specifying these settings has multiple benefits:
- It will make the search faster, as it will skip memory that is not relevant to your search.
- It will make the search more reliable, as it will avoid returning results that are not useful to you.

## Using the results

The `FindBytes` method returns an `IEnumerable<UIntPtr>` that contains the addresses of the memory locations where the pattern was found. You can then use these addresses to read or write memory, or to hook functions.

Here is an example of how to read a value at the first address found, with an offset:

```csharp
var myGame = ProcessMemory.OpenProcess("mygame");
UIntPtr targetAddress = myGame.FindBytes("4D 79 28 2A ?? ?? ?? ?? 75 65")
    .FirstOrDefault();

// The line below reads the actual value of the wildcard part of the pattern, skipping the first 4 bytes
int value = myGame.ReadInt(targetAddress + 0x4);
```

When using the results of `FindBytes`, be careful of multiple enumerations. If you need to use the results multiple times, consider storing them in a list:

```csharp
var results = myGame.FindBytes("4D 79 ?? ?? 65").ToList();
```

If you only need the first result, you can use `FirstOrDefault` as shown in the examples above. This will ensure that the search stops as soon as a match is found, which can speed up the execution dramatically, depending on your use case.

## Preventing hangs

If you are searching for a pattern in a large memory range, the search can take a long time.

If you are running this code in a UI application, your application might become unresponsive during the search.

To prevent this, you should use the asynchronous variant `FindByesAsync`, which returns an `IAsyncEnumerable`.

Here is an example of how to prevent your application from hanging during the search:

```csharp
private async Task OnButtonClick()
{
    var results = myGame.FindBytesAsync("4D 79 ?? ?? 65");
    await foreach (UIntPtr result in results)
    {
        // Do something with the result
    }
}
```

If you want to only get the first result, or work with an array or a list of results, you can use the `FirstOrDefaultAsync`, `ToArrayAsync`, or `ToListAsync` extension methods from the `System.Linq.Async` package.

```csharp
private async Task OnButtonClick()
{
    UIntPtr result = await myGame.FindBytesAsync("4D 79 ?? ?? 65").FirstOrDefaultAsync();
    // Do something with the result
}
```
