# Manipulating strings

Strings are more complicated than the basic boolean and numeric types: they usually are object instances with their own memory allocation, they have a length, an encoding, and there is no globally consistent way to handle them.

To take a couple of examples, programs using the .net framework will have strings stored with a 4-byte prefix indicating their length, and use the UTF-16 encoding and a \0 terminator, while programs made with Rust will likely use UTF-8 with no terminator and a 2-byte length prefix.

## Reading strings

In addition to the usual parameters, the `ReadString` method takes a max length and a `StringSettings` instance parameters.

## Using the right StringSettings

Once you have located the string you want to read in memory, you might find that calling the `ReadString` method with the default parameters works just as you would expect.

If the result doesn't look right, read through the "Problems and solutions" section of this guide, experiment with various presets (e.g. `StringSettings.DefaultUtf8`) or try to figure out exactly what settings to use.

To build your own `StringSettings` instance, just use the constructor and supply the following parameters:
- `encoding`: this should almost always be either `System.Text.Encoding.UTF8` or `System.Text.Encoding.Unicode` (which is UTF-16). You will recognize the latter when inspecting your target string if each character seems to take two bytes (usually, the second byte will be `00`).
- `isNullTerminated`: this specifies whether your strings are terminated with a `\0` character (the value 0, which is also called `null`). Some languages or frameworks read strings character-by-character until they encounter this terminator. Others rely on a length prefix. And sometimes you will have both. You can usually set this to `true` if you have a `00` at the end of your string, but it's usually better to set it to `false` if you have a length prefix (see the following parameter).
- `prefixSettings`: this is an instance of `StringLengthPrefixSettings`, that specifies if your string is length-prefixed, and if so, how to read that prefix. A length prefix is a number that comes right before the first character of your string and that is equal to the number of characters or bytes in your string. If your string doesn't seem to have one, set this parameter to `null`. Otherwise, you can build an instance with the given parameters:
  - `prefixSize`: set this to the number of bytes of the length prefix. It will usually be either `2` or `4`. Check how long the prefix seems to be on your string with a memory inspector.
  - `lengthUnit`: when reading a string, the number in the prefix will be multiplied by this parameter to obtain the number of bytes to read. So if the length prefix is the number of bytes, specify `1`. If the prefix reads 5 but your string is 10-bytes-long, specify `2`. If you omit this parameter or set it to `null`, it will be determined automatically.

**Here are a few additional considerations:**
- The `maxSizeInBytes` parameter in `ReadString` is always applied, no matter what `StringSettings` you specify. It also has a default value, so be aware of that if you need to read a string that might be long.
- If your `StringSettings` specify a length prefix, your pointer path needs to point to the start of the prefix, not the first character of the string. If you ignore this, you will be getting results where the first characters are missing and the string length might not be right.

### Problems and solutions

- **The result I get from `ReadString` looks like a bunch of garbled, random characters**
  - You are probably not using the right encoding. Try using `StringSettings.Default` and then `StringSettings.DefaultUtf8`. One of the two should either work, or leave you with more minor issues.
  - If the solution above does not work, check your pointer path.


- **The result I get from `ReadString` is missing a few characters at the start**
  - If your string has a length prefix, make sure that your pointer path points at the start of the length prefix, not at the first character of the string. Typically, your pointer path will end with `,C` when it should end with `,8`.
  - If your string does not have a length prefix, make sure you specify a `StringSettings` instance with a null `PrefixSettings`.


- **The result I get from `ReadString` is longer than expected**
  - Check if your string is length-prefixed, and make sure to use appropriate `StringSettings`, with a matching `StringLengthPrefixSettings` instance.
  - If your string isn't length-prefixed, use a `StringSettings` that has `IsNullTerminated` set to `true`.


- **The result I get from `ReadString` cuts at about half the length**
  - If you are using a `StringSettings` instance with a length prefix specification, try setting the `LengthUnit` of the `StringLengthPrefixSettings` instance to `2`.
  - Check the `maxLength` parameter that you supply to `ReadString` (and remember that its value is in bytes, not characters).