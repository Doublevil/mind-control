# Storing data in memory

This section explains how to allocate memory and store data in the memory of the target process using MindControl.

## What is a memory allocation?

A memory allocation is a block of memory that is reserved in the target process's address space. This memory space can be used to store data, such as structures, arrays, strings, pictures, or even code.

For example, if you want to replace a texture in a game, you have to:
1. Allocate memory in the target process to store the new texture data.
2. Write the texture data to the allocated memory.
3. Overwrite pointers to use the new texture data from the allocated memory instead of the normal one.

MindControl provides three features for working with memory allocations: allocations, reservations, and storage.

## Storage

For most use cases, you don't need to allocate memory manually. Instead, you can use the `ProcessMemory` class to store data in the target process's memory. This is done using the `Store<T>` method, which takes a value and returns a `MemoryReservation` object that represents a reservation on allocated memory (more on that later). You can then get the address of the reservation and reference it wherever you need to.

```csharp
// Store an integer value in the target process memory. This could be any other data type that you can write.
var reservationResult = processMemory.Store(42);
if (reservationResult.IsSuccess)
{
    // We can then use the Address property of the MemoryReservation
    // In this example, we read the value back from the target process memory, but you would typically write a pointer
    // to this address somewhere so that the process uses it.
    MemoryReservation reservation = reservationResult.Value;
    int value = processMemory.Read<int>(reservation.Address).ValueOrDefault();
    Console.WriteLine($"Stored value: {value}"); // Output: Stored value: 42
}
```

When calling `Store`, under the hood, MindControl will:
- Allocate a chunk of memory in the target process that is large enough to hold the data you want to store, but usually bigger than that for various reasons
- Keep track of the allocated memory in a `MemoryAllocation` object to maybe reuse it later
- Reserve a portion of the `MemoryAllocation` just big enough to hold the data you want to store, essentially creating a `MemoryReservation` object
- Write the data at the address of that reservation
- Return the `MemoryReservation` object

If we call `Store` again with some data that is small enough to fit in the same `MemoryAllocation`, MindControl will reuse the same allocation, and create a new `MemoryReservation` on it, for your new data. This is done to avoid unnecessary memory allocations and to optimize memory usage.

```csharp
var reservation1 = processMemory.Store(42).Value;
var reservation2 = processMemory.Store(64).Value;
// Only one memory allocation is issued, but two different reservations are created.
// Usually, you don't need to worry about this.
```

The advantage of using `Store` is that it abstracts away the details of memory allocation and management, allowing you to focus on the data you want to store rather than the underlying memory operations. You don't have to worry about where the memory is allocated, how much space is reserved, about the system page size, data alignment, or even about accidentally overwriting the memory you allocated.

> [!NOTE]
> Disposing the `MemoryReservation` object will automatically free the memory reserved for your data, so that it can be reused to store other data later. If your program dynamically stores more and more data, you have to make sure to dispose of the `MemoryReservation` objects when you no longer need them, to avoid memory leaks in the target process.

The two other features, allocation and reservation, are more low-level and give you more control over the memory management process. They're usually not needed, but there are cases where you might want to use them.

## Allocations

If you need to allocate memory manually, you can use the `Allocate` method of the `ProcessMemory` class. This method allows you to allocate a block of memory in the target process's address space. This method returns a `MemoryAllocation` object that represents the allocated memory.

```csharp
// Allocate a block of memory of at least 1024 bytes in the target process, to store data (not code)
MemoryAllocation allocation = processMemory.Allocate(1024, forExecutableCode: false).Result;
// The actual allocation size may be larger than 1024 bytes, depending on the system page size and other factors.
```

Note that the second parameter, `forExecutableCode`, specifies whether the allocated memory should be executable or not. If you plan to write code to this memory, you should set this parameter to `true`. Otherwise, you can set it to `false` to avoid performance overhead and potential security risks.

There are also two optional parameters that you can use to provide memory range limits for the allocation, and/or to specify that the memory allocation should be made as close as possible to a specific address. This is most useful when performing code injection and other advanced memory manipulation techniques.

> [!NOTE]
> Like the reservations, disposing `MemoryAllocation` instances will free the memory allocation in the target process. If your program creates allocations dynamically, make sure to dispose of them when you no longer need them, to avoid memory leaks in the target process.

With the returned `MemoryAllocation` object, you can perform reservations, which is the topic of the next section.

## Reservations

If an allocation is a physical block of memory in the target process, a reservation is a logical portion of that allocation that you explicitly mark as used.

Basically, reservations are another layer on top of allocations that allow you to manage your allocations more easily. When you make a reservation, you are locking in a portion of the allocated memory, and that makes sure that future reservations will not overlap with it.

There are two ways to create a reservation: using the `ReserveRange` method of the `MemoryAllocation` class, or using the `Reserve` method directly on the `ProcessMemory` class.

### Using a MemoryAllocation

```csharp
MemoryAllocation allocation = processMemory.Allocate(1024, forExecutableCode: false).Result;
// Reserve 256 bytes of that allocation
MemoryReservation reservation = allocation.ReserveRange(256).Result;
// You can now use the reservation to write data
processMemory.Write(reservation.Address, 42);
```

### Using ProcessMemory

The `Reserve` method is a more convenient way to create a reservation without having to manage the allocation yourself. It will browse existing allocations to find one that satisfies the required size, create one if none exists, and then make a reservation in it.

```csharp
// Reserve 256 bytes of memory in the target process, without manually creating a new allocation
MemoryReservation reservation = processMemory.Reserve(256, requireExecutable: false).Result;
// You can now use the reservation to write data
processMemory.Write(reservation.Address, 42);
```

## Conclusion

To recapitulate, in most cases, you can use the `Store<T>` method to store data in the target process's memory without worrying about allocations and reservations. If you need more control over memory management, you can use the `Reserve` method to create reservations directly, or, if you need even more control, you can use `Allocate` to manage allocations yourself.
