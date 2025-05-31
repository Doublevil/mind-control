# Searching for byte sequences or patterns

This section explains how to search through the memory of the target process for specific byte sequences or byte patterns. This type of search is sometimes called "AoB (Array of Bytes) scanning", and it's useful to find specific data arrangements in memory, especially in cases where pointer paths fail or are harder to pull off.

Let's jump right into an example to make things clearer.

```csharp
IEnumerable<UIntPtr> results = processMemory.SearchBytes("90 A8 00 00 ?? 42 A8");
```

In this example, we are searching for a specific byte pattern in the target process's memory. The pattern consists of some set hexadecimal byte values, and the `??` is a wildcard that matches any byte value at that position. The search will return all addresses where this pattern is found.

So this example would match, for instance, `90 A8 00 00 01 42 A8` or `90 A8 00 00 FF 42 A8`, but not `90 A8 00 01 42 A8`.

Now, this example searches the entire memory of the target process, which is usually very slow, especially if your target process uses up a lot of memory. For modern 3D games, this can easily take over a minute. Let's dive into how to make this search more efficient.

> [!NOTE]
> Do not start your patterns with a wildcard (`??`). Even though this is supported, it will slow down the search significantly, and has no practical purpose. You can remove leading wildcards from your patterns to achieve the same result without the performance hit.

## Restricting the search range

The second parameter of the `SearchBytes` method allows you to specify a range of memory addresses to search in. This can significantly speed up the search process, especially if you know where the data you're looking for is likely to be located.

```csharp
MemoryRange range = new MemoryRange(0x10000000, 0x20000000);
IEnumerable<UIntPtr> results = processMemory.SearchBytes("90 A8 00 00 ?? 42 A8", range);
```

The typical way to use this parameter is to get a specific module in the target process, and then search only within that module's memory range. Here's how:

```csharp
RemoteModule module = processMemory.GetModule("GameAssembly.dll") ?? throw new Exception("Module not found");
IEnumerable<UIntPtr> results = processMemory.SearchBytes("90 A8 00 00 ?? 42 A8", module.GetRange());
```

## Specifying settings to filter out invalid results

Another way to both speed up the search *and* filter out unwanted results is to use the third `FindBytesSettings` parameter. This allows you to specify additional criteria for the search, to ignore certain ranges of memory depending on their properties, or to specify a maximum number of results.

```csharp
RemoteModule module = processMemory.GetModule("GameAssembly.dll") ?? throw new Exception("Module not found");
var settings = new FindBytesSettings
{
    SearchReadable = true, // Only search in readable memory
    SearchWritable = null, // Don't care if the memory is writable or not
    SearchExecutable = false, // Ignore executable memory, because we are looking for data, not code
    MaxResultCount = 10 // Limit the number of results to 10
};
IEnumerable<UIntPtr> results = processMemory.SearchBytes("90 A8 00 00 ?? 42 A8", range, settings);
```

Using these settings can greatly improve the performance of your searches, but keep in mind that they are still extremely slow compared to pointer paths or direct memory reads. Use them only when you believe this is the best way to find the data you are looking for.
