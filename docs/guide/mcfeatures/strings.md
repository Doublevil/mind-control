# Manipulating strings

Reading and writing strings in a process's memory is more complex than numeric values and requires understanding how strings are represented in memory. In this section, we will cover how strings are stored in memory, and how to manipulate them with `MindControl`.

## Understanding string representation in memory

Intuitively, strings are sequences of characters, but in memory, they are represented as a sequence of bytes. In some way or another, these bytes represent the characters in the string. But because their length is variable, **they are almost always referenced by a pointer**. For example, a `Player` structure could have a `Name` field that holds a pointer to the start of the string in memory.

Now, once you locate a string, the key is then to understand how to translate its bytes into characters and vice versa.

### Encodings

The thing is, there are many ways to represent strings in memory, and it mostly comes down to what is called the "encoding". The encoding defines how characters are mapped to bytes. Some common encodings are:
- **ASCII**: Uses 1 byte per character, supports only the first 128 characters (English letters, digits, and some symbols).
- **UTF-8**: Uses 1 to 4 bytes per character, supports all Unicode characters, like symbols, chinese characters, emojis, etc. It is the most common encoding used in modern applications.
- **UTF-16**: Uses 2 bytes per character, also supports all Unicode characters, and is commonly used in Windows applications.

You may also see other encodings like UTF-32, ISO-8859-1, or others. It all depends on the application and how it was developed. And in a single application, you may find different encodings used for different use cases, because some encodings are more efficient for certain types of data. For example, you may decide to use UTF-16 for a player name, to handle any language, but still use ASCII for internal item identifiers, because you don't need more than the English alphabet, and it uses up less memory.

Let's take a look at some examples of how the same string is represented in memory with different encodings.

#### Example: "Hello, World!" in different encodings

| Encoding | Bytes in Hexadecimal |
| --- | --- |
| ASCII | 48 65 6C 6C 6F 2C 20 57 6F 72 6C 64 21 |
| UTF-8 | 48 65 6C 6C 6F 2C 20 57 6F 72 6C 64 21 |
| UTF-16 | 48 00 65 00 6C 00 6C 00 6F 00 2C 00 20 00 57 00 6F 00 72 00 6C 00 64 00 21 00 |

#### Example 2: "こんにちは" (Hello in Japanese) in different encodings

| Encoding | Bytes in Hexadecimal                         |
| --- |----------------------------------------------|
| ASCII | (This string cannot be represented in ASCII) |
| UTF-8 | E3 81 93 E3 82 93 E3 81 AB E3 81 A1 E3 81 AF |
| UTF-16 | 53 30 93 30 6B 30 61 30 6F 30                |

We can see several interesting things here:
- The ASCII and UTF-8 representations of "Hello, World!" are the same, because all characters in this string are part of the ASCII character set, and UTF-8 is designed to be backward compatible with ASCII.
- The Japanese string cannot be represented in ASCII, because it contains characters that are not part of the ASCII character set. However, it can be represented in both UTF-8 and UTF-16.
- UTF-16 always uses 2 bytes per character, which is kind of wasteful for ASCII-compatible strings like "Hello, World!" (you can see every second bit is a zero), but it ends up using way less space for the Japanese string. This is because it _always_ uses 2 bytes per character, while UTF-8 has variable-length characters, meaning it has to dedicate extra bits to indicate one way or another how many bytes are used for each character in the string.

So, after locating a string in memory, we need to find out what encoding is used to represent it.

### Knowing where the string stops

When reading a string from memory, we also need to know where it ends. Because bytes in memory are not delimited and keep going on pretty much forever, it's important to know when to stop reading. And to do that, there are 2 main ways that programming languages and libraries use:
- **Null-terminators**: This is a special byte (`00`) or group of bytes (`00 00`) appended after the final character that indicates the end of the string. When reading a string, you keep reading bytes until you encounter a null terminator. This is common in C-style strings and is used in many programming languages.
- **Length prefix**: This is a byte or group of bytes before the first character of the string, that indicates how many bytes or characters follow it. When reading a string, you read the length first, and then read that many bytes. This is common in stacks like .net and Java.

Sometimes, both techniques are used together, for compatibility.

#### Example 1: Reading a string with a null terminator

Let's say we have a string "Hello, World!" stored in memory as UTF-8, and we know it is null-terminated. The bytes in memory would look like this:

```
48 65 6C 6C 6F 2C 20 57 6F 72 6C 64 21 00
```

When reading this string, we would know to stop reading as soon as we encounter the null terminator `00`. The resulting string would be "Hello, World!".

#### Example 2: Reading a string with a length prefix

Now, let's say we have the same string "Hello, World!" stored in memory as UTF-8, but this time we have identified that it has a length prefix of 2 bytes. The bytes in memory would look like this:

```
0D 00 48 65 6C 6C 6F 2C 20 57 6F 72 6C 64 21
```

When reading this string, knowing that it has a length prefix of 2 bytes, we would start by reading the length prefix `0D 00`. This is the number 13, meaning that the string is 13 bytes long. Then we would know to read only the next 13 bytes, resulting in the string "Hello, World!".

### Type handles

In some cases, especially in managed languages and frameworks like .net, strings, like any other object instance, start with a type handle. This is a pointer to a metadata structure that describes the type of the object (in this case, a string). This type handle is used for various purposes by the runtime.

We don't care about this handle, but, when reading a string, we need to know if it exists and how long it is, so we can skip it. And when writing, we actually need to know the full handle, so we can write it properly, the way it would have been written by the runtime.

#### An example of a typical .net string

The .net standard for strings has all the things we've discussed so far, which makes it ideal for a final example. So let's take a look at a "Hello, World!" string in .net:

```
C0 12 34 56 78 9A BC DE 0D 00 00 00 48 00 65 00 6C 00 6C 00 6F 00 2C 00 20 00 57 00 6F 00 72 00 6C 00 64 00 21 00 00 00
```

This string is represented in memory as follows:
- `C0 12 34 56 78 9A BC DE`: This is the .net type handle, which is a pointer to the metadata structure that describes the string type. This is an example, the actual handle will be different in every application, and even in every run of the same application.
- `0D 00 00 00`: This is the length prefix. In .net, length prefixes are 4 bytes long, and indicate the number of characters. This one reads as 13 (0x0D) characters long.
- `48 00 65 00 6C 00 6C 00 6F 00 2C 00 20 00 57 00 6F 00 72 00 6C 00 64 00 21 00`: These are the UTF-16 encoded characters of the string "Hello, World!", with each character taking up two bytes.
- `00 00`: This is the null terminator, which indicates the end of the string. Because we are using UTF-16, the null terminator is represented as two bytes and not just one. We technically don't need it, because we have the length prefix, but .net still has this terminator for compatibility with other systems that expect it.

## Reading strings with MindControl

There are several options to read strings in MindControl, depending on your needs and preferences.

### Using ReadStringPointer

The `ReadStringPointer` method of the `ProcessMemory` class is the most versatile way to read strings from memory. It takes an address or a pointer path to the pointer to the string you want to read, along with a `StringSettings` object that defines how the string is represented in memory. This method will read the pointer, then read the string from the address it points to, and return the string value.

Let's take an example to read the string from the previous example (a standard .net string):

```csharp
var stringSettings = new StringSettings(
    encoding: System.Text.Encoding.UTF8,
    isNullTerminated: true,
    lengthPrefix: new LengthPrefix(4, StringLengthUnit.Characters), // 4 byte length, counting characters (not bytes)
    typePrefix: new byte[] { 0xC0, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE });

string? result = processMemory.ReadStringPointer("GameAssembly.dll+12345678", stringSettings).ValueOrDefault();
```

In this example, we specify the encoding as UTF-8, the string is null-terminated, and it has a 4-byte length prefix that counts characters. We also provide the type handle as a byte array. The method will read the pointer at the specified address, then read the string from the address it points to, and return the string value.

> [!NOTE]
> As stated before, this method takes the address of a pointer to the string, **not** the address of the start of the string itself.

### Using ReadRawString

An alternative to `ReadStringPointer` is the `ReadRawString` method, which reads a string directly from the address or pointer path to its first character byte, without needing to provide a pointer to the string. This method is useful when you don't have a pointer to the string, or if you prefer to read the string directly from its starting address.

Instead of a `StringSettings` object, this method takes an `Encoding` parameter to specify how the string is encoded, a max length that indicates when to stop, and a boolean to indicate if the string is null-terminated.

A matching example for the previous string would look like this:

```csharp
// Note the 'C' offset at the end of the pointer path to skip the type handle (8 bytes) and the length prefix (4 bytes)
string? result = processMemory.ReadRawString("GameAssembly.dll+12345678,C",
    Encoding.UTF8,
    maxLength: 100,
    isNullTerminated: true).ValueOrDefault();
```

> [!NOTE]
> ReadRawString stops reading when it reaches the specified maximum length, or when it encounters a null terminator, whichever comes first. If you specify no null terminator, it will always read up to the maximum length.

### Determining the string settings automatically

If you don't know the string settings, you can use observation and trial and error to determine them. However, `MindControl` provides a method that helps you pinpoint the string settings automatically. The `FindStringSettings` method takes an address or pointer path to a pointer to a string, and the expected string value, and returns a `StringSettings` object that matches the string representation in memory.

It may not be ideal for all cases, especially because it requires you to know a string value that you expect to find in memory, but it can be useful especially when dealing with strings with a type handle that changes every time the application is run.

```csharp
// Determine the string settings automatically thanks to a known "Hello, World!" string somewhere in memory
var expectedString = "Hello, World!";
StringSettings stringSettings = processMemory.FindStringSettings("GameAssembly.dll+12345678", expectedString)
    .ValueOrDefault();

// Now you can use the string settings to read another string that you don't know
string? anotherString = processMemory.ReadStringPointer("GameAssembly.dll+ABCDEF012", stringSettings).ValueOrDefault();
```

## Writing strings with MindControl

By design, MindControl does not have a method that directly writes strings to memory. This is because it would make it too easy for users to make mistakes and either corrupt memory, or bloat memory with hundreds of thousands of strings that are never erased.

This is because, unless you are certain that your new string is smaller than the original string you want to replace, you cannot simply overwrite the bytes of the original string with the bytes of your new string. If the new string is longer, you would end up writing past the end of the original string, which could corrupt memory and lead to crashes or unexpected behavior.

Instead, you can write strings to memory by chaining multiple operations:
- (Optional) Use `FindStringSettings` on a known string to determine the string settings for the string you want to write.
- Use `StoreString` to store the value of the string somewhere in an allocated space in memory.
- Use `Write` to overwrite the pointer to the string with the address of your newly stored string.

Here is a concrete example of exactly that:

```csharp
// Determine the string settings automatically thanks to a known "Hello, World!" string
var expectedString = "Hello, World!";
StringSettings stringSettings = processMemory.FindStringSettings("GameAssembly.dll+12345678", expectedString)
    .ValueOrDefault();
    
// Store the new string in memory using the determined string settings
MemoryReservation reservation = processMemory.StoreString("New String Value", stringSettings).ValueOrDefault();

// Write the pointer to the new string in the target process memory
processMemory.Write("GameAssembly.dll+12345678", reservation.Address).ThrowOnFailure();
```

> [!NOTE]
> Make sure to dispose of the `MemoryReservation` object when you no longer need it, to avoid memory leaks in the target process. If you constantly write new strings in this way without disposing of the old reservations, you will quickly end up consuming gigabytes of memory in the target process, which will lead to performance issues and crashes.
