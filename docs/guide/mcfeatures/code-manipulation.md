# Manipulating code

This section is a bit more advanced and will explain how to manipulate code in the target process using MindControl.

> [!NOTE]
> This section requires the `MindControl.Code` package to be installed in addition to the `MindControl` package.

## What is code manipulation?

Code manipulation refers to the ability to modify the executable code of a running process. This can include removing or changing instructions, injecting new code, or redirecting execution flow. Code manipulation is often used in game hacking, reverse engineering, and debugging scenarios.

Executable code is stored in the process memory, just like any other data, under the form of instruction bytes called opcodes. These opcodes are executed by the CPU to perform various operations, such as arithmetic calculations, memory access, and control flow changes (jumping to a different instruction, often depending on various conditions).

Manipulating code can be challenging, because messing up a single bit in an instruction often leads to crashes or unexpected behavior. Injecting new code without modifying the existing code is especially difficult, because you cannot just insert new instructions in the middle of existing code without breaking the flow of execution.

## Opcodes

Opcodes are the machine-level instructions that the CPU executes. Each opcode corresponds to a specific operation, usually very basic, such as adding two numbers, jumping to a different instruction, or calling a function. Opcodes are represented as byte sequences in memory, and they can vary depending on the CPU architecture (e.g., x86, x64).

When using a memory hacking tool such as Cheat Engine, you can view the disassembled code, which shows the opcodes in a human-readable format (assembly language). This allows you to see what the code is doing and how it is structured, but it is still very complex to understand, because meaningful operations (like "fire a bullet") are made of thousands of opcodes, each performing a basic operation that has no obvious meaning by itself.

Here are some examples of common opcodes in x64 architecture:
```assembly
mov eax, 1          ; B8 01 00 00 00 - Move the value 1 into the EAX register.
add eax, 2          ; 05 02 00 00 00 - Add the value 2 to the EAX register.
jmp 0x12345678      ; (Bytes depend on multiple factors) - Jump to the instruction at address 0x12345678.
call 0x12345678     ; (Bytes depend on multiple factors) - Call the function at address 0x12345678
nop                 ; 90 - No operation, does nothing.
```

Note that the opcodes are not always the same length, and some operations can take different forms depending on the operands used. For example, the `jmp` and `call` instructions can have different byte sequences depending on whether they use absolute addresses, relative offsets, or other addressing modes.

> [!NOTE]
> When diving into code manipulation, it is very advisable to learn the basics of assembly language. This is outside the scope of this guide, but there are many resources available online to help you get started.

## Removing code instructions

The easiest code manipulation operation is to remove code instructions. This can be done by overwriting the instructions with `NOP` (No Operation) instructions, which effectively make the code do nothing.

Fortunately, `NOP` instructions are only one byte long, so you can replace any instruction with a number of `NOP` instructions without changing the size of the code. For example, removing a `mov eax, 1` instruction would be as simple as writing 5 bytes of `0x90` (the opcode for `NOP`) at the address of the instruction.

MindControl provides easy-to-use methods to remove code instructions. The `DisableCodeAt` method disables a number of instructions at a specific address by overwriting them with `NOP` instructions.

```csharp
// Disable 5 instructions, starting at the address "mygame.exe+0168EEA0"
CodeChange codeRemoval = processMemory.DisableCodeAt("mygame.exe+0168EEA0", 5);

// Disposing the CodeChange object will restore the original code
codeRemoval.Dispose();
```

> [!NOTE]
> It is important to consider that removing code instructions can lead to unexpected behavior, especially if the removed instruction is part of a larger control flow structure (like a loop or a conditional statement). If you don't know what the removed instructions do, chances are you will cause a crash.

## Injecting code

Injecting code is a more advanced operation that allows you to add new instructions to the target process. This can be used to implement custom functionalities or to modify existing behavior.

Because we cannot just insert new instructions in the middle of existing code and shift everything around, this is usually done through a hook. A hook is a technique that intercepts the execution flow of the target process and redirects it to your custom code, usually redirecting it back to the original code afterward.

The steps are the following:
1. Allocate executable memory in the target process to store the new code. Ideally, the code is located near the original code, to optimize performance. 
2. Write the new code to the allocated memory.
3. Overwrite the original code at the address of the target instruction with a jump instruction that redirects execution to the new code.

Typically, the code you write at step 2 will end with a jump instruction that redirects execution back to the original code, so that the original code can continue executing after your custom code has run. This is often referred to as a "trampoline". (So now you know how to build trampoline hooks.)

If you want to inject code without replacing functionality, the code you write at step 2 may start with whatever instructions end up being replaced by the jump instruction at step 3.

There is a big issue with that though. The code you write will often use registers and set CPU flags, meaning that, when you redirect execution back to the original code, the state of the CPU will not be what the original code expects. This often leads to crashes or unexpected behavior. To protect against this, you need to save the state of the CPU before executing your custom code, and restore it afterward. This is often done by pushing the registers onto the stack at the start of your custom code, and popping them back at the end.

We won't go into the details of how to write assembly code for this, but we will see how to do it using MindControl.

Whatever you want to achieve, MindControl provides three ways to inject code.

### Using InsertCodeAt

The `InsertCodeAt` method of `ProcessMemory` allows you to inject code at a specific instruction in the target process. It takes either the address of a pointer path to the address of an instruction, and the code to inject. In this variant, the code is going to be executed before the original instruction, and the original instruction will be executed afterward. No instructions are removed, the original code is preserved entirely.

The code you provide is either a byte array, or a `Iced.Intel.Assembler` object that contains some code ready to be assembled. The `Iced.Intel.Assembler` class is part of the Iced project, which is a library for disassembling and assembling x86/x64 code. You can use it to line up assembly code in your .net project that you can then inject through MindControl.

```csharp
// Create an assembler and write some code to it
var assembler = new Assembler(64);
assembler.mov(rcx, value);
// ...

// Insert the code at the address "mygame.exe+0168EEA0"
CodeChange codeInjection = processMemory.InsertCodeAt("mygame.exe+0168EEA0", assembler).Value;

// Disposing the CodeChange object restores the original code and frees the memory reservation where code was written
codeInjection.Dispose();
```

### Using ReplaceCodeAt

Similarly, the `ReplaceCodeAt` method allows you to replace one or more instructions with your own code. This is useful when you want to modify the behavior of existing code.

The differences with `InsertCodeAt` are:
- You can specify the number of instructions to replace, and the code you provide will be executed **instead of** the original instructions.
- If your code is shorter or equal in size to the original instructions, the original code will simply be overwritten. If your code is longer, a hook will be performed. You don't have to worry about that.

```csharp
// Create an assembler and write some code to it
var assembler = new Assembler(64);
assembler.mov(rcx, value);
// ...

// Replace 3 instructions, starting at the instruction at address "mygame.exe+0168EEA0", with the code we just prepared
CodeChange codeInjection = processMemory.ReplaceCodeAt("mygame.exe+0168EEA0", 3, assembler).Value;

// Disposing the CodeChange object restores the original code and frees the memory reservation where code was written
// (in cases where a hook was necessary).
codeInjection.Dispose();
```

### Using Hook

Finally, the `Hook` method allows you to specify what kind of hook to perform, through a `HookOptions` object. This method provides slightly more control over the hook, but it's almost always possible to achieve the same result using either `InsertCodeAt` or `ReplaceCodeAt`.

```csharp
// Create an assembler and write some code to it
var assembler = new Assembler(64);
assembler.mov(rcx, value);
// ...

// Create hook options to specify the type of hook to perform
HookOptions hookOptions = new HookOptions(HookExecutionMode.ExecuteInjectedCodeFirst);

// Hook the instruction at address "mygame.exe+0168EEA0" with the code we just prepared
CodeChange codeInjection = processMemory.Hook("mygame.exe+0168EEA0", assembler, hookOptions).Value;

// Disposing the CodeChange object restores the original code and frees the memory reservation where code was written
codeInjection.Dispose();
```

> [!NOTE]
> It's generally discouraged to use the `Hook` method directly, as it is more complex and less intuitive than the other two methods. The `InsertCodeAt` and `ReplaceCodeAt` methods are usually better and easier to read.

### Code isolation

As we have previously touched, injecting code can lead to unexpected behavior if the injected code does not properly handle the CPU state. To make sure that your injected code does not interfere with the original code, you have to save and restore the CPU registers and flags before and after executing your custom code.

You can do this manually by pushing the registers onto the stack at the start of your custom code, and popping them back at the end. However, MindControl provides a more convenient way to do this through an additional parameter in both `InsertCodeAt` and `ReplaceCodeAt`.

This parameter is an array of `HookRegister`, an enumeration that you can use to list the registers that your code uses. When performing the code manipulation, MindControl will automatically save and restore the state of these registers.

Additionally, if your injected code is very complex or if you want to make sure that it does not interfere with the original code, you can use one of the pre-made arrays available through the `HookRegisters` static class. For example, `HookRegisters.AllCommonRegisters` is a pre-made array of all commonly used registers, and using it in a code manipulation operation will almost always guarantee that your code does not interfere with the original code.

In performance-critical scenarios, you should try to list only the registers that your code actually uses, to avoid the overhead of saving and restoring unnecessary registers.

#### Example using a few registers

```csharp
// Save and restore the state of the RCX and RBX registers, and the CPU flags
CodeChange codeInjection = processMemory.InsertCodeAt("mygame.exe+0168EEA0", assembler,
    HookRegister.RcxEcx, HookRegister.RbxEbx, HookRegister.Flags).Value;
```

#### Example using all common registers

```csharp
// Using all common registers is a catch-all solution, but runs slower
CodeChange codeInjection = processMemory.InsertCodeAt("mygame.exe+0168EEA0", assembler,
    HookRegisters.AllCommonRegisters).Value;
```

> [!NOTE]
> In the `HookRegister` enumeration, x64 and x86 versions of the same register are grouped together, because MindControl is not compiled against a specific architecture. For example, `HookRegister.RcxEcx` refers to the x64 `RCX` register if your target process is x64, or the x86 `ECX` register if your target process is x86.