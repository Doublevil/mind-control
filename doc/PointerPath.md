# PointerPath expressions

This guide will give you a comprehensive look at how to use the `PointerPath` class and its expressions.

The goal of a pointer path is to detail a path in memory that you can follow to get to the address of a targeted value, by reading a series of pointers. They are used as an alternative to static addresses, that are usually not stable (meaning they do not always point to the right value in memory).

Read the "Quick look" section below to learn about pretty much everything you need to know. There is usually no need to go further than that in most scenarios.

## Quick look

Here is an example of a PointerPath expression:

`"mygame.exe"+1D5A10,1C,8`

Let's decompose it:
- `"mygame.exe"`
 
This is a module name. Most of the time, you should work with module names to get stable pointers. Modules are files loaded in memory at a particular address. This gives us a base address to start with.

- `+1D5A10`

This is the base module offset. It applies to the address of the module. In this case, our first pointer is at the address of the `"mygame.exe"` module, plus 1923600 (1D5A10) bytes.

- `,1C`

This is the offset to the second pointer in the path. You can tell by the `,` which separates each pointer to follow. In this case, the second pointer is 28 (1C) bytes after following the first one.

- `,8`

This is the offset to the third and final pointer in the path. The last pointer is 8 bytes after following the second one.

### Evaluating the pointer path

Evaluating a pointer path is the process of translating it to a static memory address, that we can read from or write to. Pointer paths are evaluated every time they are used.

When evaluating our example pointer path, this is what happens:
1. The address of the `"mygame.exe"` module is determined through the process' module list
2. The first offset `1D5A10` is added to the address of the module
3. A pointer is read from the memory at the resulting address.
4. The second offset `1C` is added to the pointer read at the previous step.
5. A pointer is read from the memory at the resulting address.
6. The third offset `8` is added to the pointer read at the previous step.

This gives us our final address that we can read from or write to.

## Full syntax guide

This section is here for a more comprehensive syntax breakdown. You should be able to handle most cases with the information in the previous section, but keep reading if you want to learn more advanced use cases.

### Structure

A PointerPath expression is basically comprised of a starting address, and a series of offsets. Each part is separated with a `,`.

#### Starting address syntax

An expression will always start with a starting address, that provides a first address to start with. It cannot contain any additional starting addresses.

The starting address uses the same syntax as an offset, except it can also start with a module name.

Typically, a module name looks like `"mygame.exe"`, but here are some other valid module names: `"mygame.data"`, `mygame.exe`, `mygame`, `my game`, `"1F8D"`.

This highlights a few things:
- A module name does not necessarily start and end with `"` (unless it's otherwise a valid hexadecimal number as in that last example). These `"` characters are trimmed during evaluation to find out the actual module name.
- The module name may have any extension, or no extension at all.
- The module name may contain spaces.

In addition to the module name, a starting address may, like any offset, have any number of static offsets chained together with `+` or `-` operators. Here are some valid examples of a starting address: `"mygame.exe"+0F`, `mygame.exe-0F`, `mygame+1C-4+2`. See the "Offset syntax" section below for more info.

It shall be noted that a starting address does not necessarily start with a module name. It may also be any static address. Here is a couple of examples of full, valid PointerPath expressions with no module name: `1F016644,13,A0`, `1F016644+13,A0`.

#### Offset syntax

Any number of offset expressions can follow the starting address expression.

An offset expression is comprised of at least one hexadecimal number. It can be added together with others, through a series of algorithmic `+` or `-` operators.

Additionally, an offset expression can start with a `-` sign to indicate a negative offset.

Here are a few examples of valid offsets and what they evaluate to:
- `2A` evaluates to `2A`
- `2A+4` evaluates to `2E`
- `2A-3` evaluates to `27`
- `2A+4-4+2` evaluates to `2C`
- `-2A` evaluates to `-2A`

Two operators cannot be chained, and an offset cannot end with an operator.