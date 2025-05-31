# Executing remote functions

This section covers how to execute functions in your target process. This can allow you to perform actions in the target process from your program, or to call your own functions after injecting a DLL into the target process.

You can execute functions with the method `RunThread`. There are two main ways to call this method:

## With an address or pointer path

You can call a function by providing its address or a pointer path to it. This is useful when you know the exact location of the function in memory.

```csharp
var result = processMemory.RunThread("UnityPlayer.dll+0168EEA0,8,100,28,20,80");
result.ThrowOnFailure(); // Throws an exception if the function could not be executed

// Wait for the function to complete, with a timeout of 10 seconds
result.Value.WaitForCompletion(TimeSpan.FromSeconds(10));

// Dispose the result when done to ensure resources are released
result.Dispose();
```

## With a module and function name

You can also call a function by providing the module name and the function name. This is useful when you want to call a function in a specific module without needing to know its address. This uses the module's export table to find the function, and thus requires the module to explicitly export the function.

In the following example, we call the `ExitProcess` function from the Windows kernel library `kernel32.dll`. This module is loaded in every Windows process, so you can use its functions without having to inject a DLL.

```csharp
var result = processMemory.RunThread("kernel32.dll", "ExitProcess");
result.ThrowOnFailure(); // Throws an exception if the function could not be executed

// Wait for the function to complete, with a timeout of 10 seconds
result.Value.WaitForCompletion(TimeSpan.FromSeconds(10));

// Dispose the result when done to ensure resources are released
result.Dispose();
```

## Function parameters

You can also pass parameters to the function you are calling. This is an advanced feature that requires you to understand how the target function expects its parameters to be passed. In MindControl, parameters are passed through a single pointer. You can arrange your parameters in a structure, store them in memory (e.g. using `Store<T>`), and then pass the pointer to that memory as the parameter.

```csharp
struct MyFunctionParams { public int Param1; public float Param2;}
var myParams = new MyFunctionParams { Param1 = 42, Param2 = 3.14f };
var paramsPointer = processMemory.Store(myParams).Value.Address;
var result = processMemory.RunThread("myprocess.exe+019BAEA1", paramsPointer);
result.Value.WaitForCompletion(TimeSpan.FromSeconds(10));
result.Dispose();
```

However, this will only work for a specific argument passing convention. What this does is store the pointer in the RCX register (or EBX for 32-bit processes) before calling the function.

If the function is not designed to accept parameters in this way, you will need to use a different approach, such as writing a wrapper function that prepares the parameters and calls the target function (also called a trampoline).

Here is an example where we call the `GetCurrentDirectoryW` function from `kernel32.dll`, which expects two parameters: a buffer size and a pointer to a buffer where the current directory will be stored.

> [!NOTE]
> The following example is complex and requires some understanding of assembly code and registers. For most use cases, you won't need to perform trampoline calls. Don't worry about this unless you need it.

```csharp
// Define the structure that holds the parameters for GetCurrentDirectoryW
// The attribute prevents the compiler from adding padding bytes, ensuring the structure is packed tightly in memory.
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct GetCurrentDirectoryWArgs { public uint BufferSize; public ulong BufferAddress; }

// Reserve memory for the buffer where the current directory will be stored
var bufferReservation = processMemory.Reserve(2048, false).Value;

// Create an instance of the structure and store it in memory
var args = new GetCurrentDirectoryWArgs { BufferSize = 2048, BufferAddress = bufferReservation.Address };
var argsReservation = processMemory.Store(args).Value;

// Retrieve the address of the GetCurrentDirectoryW function
var kernel32Module = processMemory.GetModule("kernel32.dll");
var functionAddress = kernel32Module!.ReadExportTable().Value["GetCurrentDirectoryW"];

// Using Iced.Intel, prepare the trampoline code to call GetCurrentDirectoryW
// Make sure to use "using static Iced.Intel.AssemblerRegisters;" to use the registers in a readable way
var assembler = new Assembler(64);
// In x64, the function uses the fastcall calling convention, i.e. RCX and RDX are used for the two arguments.
// When the thread is created, the thread parameter is in RCX. In this case, our parameter is going to be the
// address of a GetCurrentDirectoryWArgs struct holding the parameters we want.
assembler.mov(rax, rcx); // Move the address of the GetCurrentDirectoryWArgs struct to RAX, to free up RCX
assembler.mov(ecx, __dword_ptr[rax]); // Move the buffer size (first argument) to ECX/RCX
assembler.mov(rdx, __[rax+4]); // Move the buffer address (second argument) to RDX
assembler.call(functionAddress.ToUInt64()); // Call GetCurrentDirectoryW
assembler.ret();

// Store the trampoline code in the target process memory
// The nearAddress parameter is used to favor allocations close to the kernel32.dll module, for more efficient jumps
// This code requires the MindControl.Code package
var codeReservation = processMemory.StoreCode(assembler, nearAddress: kernel32Module.GetRange().Start).Value;

// Now we can run the trampoline code in a new thread, passing the address of the GetCurrentDirectoryWArgs struct
var threadResult = processMemory.RunThread(codeReservation.Address, argsReservation.Address);
threadResult.Value.WaitForCompletion(TimeSpan.FromSeconds(10)).ThrowOnFailure();

// Read the resulting string from the allocated buffer
var resultingString = processMemory.ReadRawString(bufferReservation.Address, Encoding.Unicode, 512).Value;
Console.WriteLine($"Current Directory: {resultingString}"); // This should print the directory of the target process

// Dispose everything to ensure resources are released
bufferReservation.Dispose();
argsReservation.Dispose();
codeReservation.Dispose();
threadResult.Dispose();
```
