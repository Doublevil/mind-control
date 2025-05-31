# Understanding Results

Most methods in `MindControl` return a `Result` or `Result<T>` object, which encapsulates the success or failure of the operation, along with any relevant data or error messages. This allows you to handle errors gracefully and provides a consistent way to check the outcome of operations.

For example, when you read a value from a process, you can check if the operation was successful and retrieve the value if it was:

```csharp
Result<int> readResult = processMemory.Read<int>("GameAssembly.dll+12345678");
```

Instead of directly returning an `int`, and throwing an exception or returning `0` on failure, we get a `Result<int>`. You can then check if the read operation was successful using the `IsSuccess` property:

```csharp
if (readResult.IsSuccess)
{
    int value = readResult.Value;
    // Use the value as needed
}
else
{
    // Handle the error
    Console.WriteLine($"Error reading value: {readResult.Failure.Message}");
}
```

This pattern is used throughout the `MindControl` library, allowing you to handle errors in a consistent way without relying on exceptions for control flow.

Even though relying on exceptions is generally discouraged, because they are slower and tend to make the code harder to read, you can use the `ThrowOnFailure()` method to throw an exception if the operation failed, which can be useful in scenarios where you want to enforce error handling:

```csharp
readResult.ThrowOnFailure(); // Throws an exception if the read operation failed
int value = readResult.Value; // After the exception check, you can safely use the value
```

> [!NOTE]
> Accessing the `Value` property of an unsuccessful `Result<T>` will also throw an exception.

If you prefer to discard errors and just use default values, you can use the `ValueOrDefault()` method, which returns the value if the operation was successful, or a default value (like `0` for numeric types) if it failed:

```csharp
int value = processMemory.Read<int>("GameAssembly.dll+12345678").ValueOrDefault();
// value will be 0 if the read operation failed
```

Alternatively, you can use `ValueOr()` to provide a custom default value:

```csharp
int value = processMemory.Read<int>("GameAssembly.dll+12345678").ValueOr(42);
// value will be 42 if the read operation failed
```

## The Failure object

When an operation fails, the `Result` object contains a `Failure` property that provides detailed information about the error. The `Failure` base class itself contains only a `Message` property, that describes the error, but methods usually return a more specific type of `Failure` that provides additional context.

```csharp
// Ask the user for a pointer path
string pointerPath = Console.ReadLine();

// Use the pointer path to read a value
var readResult = processMemory.Read<int>(pointerPath);
if (!readResult.IsSuccess)
{
    // Handle the error in a different way based on the specific failure type
    // In this example, we just print a different message for each failure type, but the idea is that you can perform
    // different actions based on the type of failure if you need to.
    readResult.Failure switch
    {
        BaseModuleNotFoundFailure f => Console.WriteLine($"The module you entered ({f.ModuleName}) is invalid!"),
        DetachedProcessFailure _ => Console.WriteLine($"The process has exited."),
        IncompatiblePointerPathBitnessFailure _ => Console.WriteLine("The pointer path you entered is not compatible with the 32-bit target process!"),
        _ => Console.WriteLine($"An unexpected error occurred: {readResult.Failure}")
    };
    return;
}

int value = readResult.Value;
// (Use the value as needed)
```
