using System.Text.RegularExpressions;
using MindControl.Results;

namespace MindControl;

/// <summary>
/// Represents a pattern of bytes to search for in memory.
/// </summary>
public class ByteSearchPattern
{
    /// <summary>Regular expression pattern to match a valid byte pattern string. </summary>
    private static readonly Regex BytePatternRegex = new("^([0-9A-F?]{2})*$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    /// <summary>Original pattern string that was used to create this instance.</summary>
    private readonly string _originalPatternString;
    
    /// <summary>
    /// Gets the byte array part of the pattern.
    /// </summary>
    public byte[] ByteArray { get; }
    
    /// <summary>
    /// Gets the byte mask part of the pattern.
    /// </summary>
    public byte[] Mask { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ByteSearchPattern"/> from a pattern string.
    /// Prefer <see cref="TryParse"/> for better error handling in cases where your pattern may be invalid. 
    /// </summary>
    /// <param name="patternString">String representation of the byte pattern to find. This pattern should be a series
    /// of hexadecimal bytes, optionally separated by spaces. Each character, excluding spaces, can be a specific value
    /// (0-F) or a wildcard "?" character, indicating that the value to look for at this position could be any value.
    /// An example would be "1F ?? 4B 00 ?6". Read the documentation for more information.</param>
    /// <exception cref="ArgumentException">Thrown when the pattern string is invalid.</exception>
    public ByteSearchPattern(string patternString)
    {
        var parseResult = ParsePatternString(patternString);
        if (parseResult.IsFailure)
            throw new ArgumentException(parseResult.Error.ToString(), nameof(patternString));
        
        _originalPatternString = patternString;
        (byte[] byteArray, byte[] mask) = parseResult.Value;
        ByteArray = byteArray;
        Mask = mask;
    }

    /// <summary>
    /// Creates a new instance of <see cref="ByteSearchPattern"/> from a byte array and a mask array.
    /// </summary>
    /// <param name="originalPatternString">Original pattern string that was parsed into byte arrays.</param>
    /// <param name="byteArray">Byte array part of the pattern.</param>
    /// <param name="mask">Byte mask part of the pattern.</param>
    protected ByteSearchPattern(string originalPatternString, byte[] byteArray, byte[] mask)
    {
        _originalPatternString = originalPatternString;
        ByteArray = byteArray;
        Mask = mask;
    }
    
    /// <summary>
    /// Attempts to parse a pattern string into a <see cref="ByteSearchPattern"/> instance.
    /// </summary>
    /// <param name="patternString">String representation of the byte pattern to find. This pattern should be a series
    /// of hexadecimal bytes, optionally separated by spaces. Each character, excluding spaces, can be a specific value
    /// (0-F) or a wildcard "?" character, indicating that the value to look for at this position could be any value.
    /// An example would be "1F ?? 4B 00 ?6". Read the documentation for more information.</param>
    /// <returns>A result holding either the parsed <see cref="ByteSearchPattern"/>, or an instance of
    /// <see cref="InvalidBytePatternFailure"/> detailing the reason for the failure.</returns>
    public static Result<ByteSearchPattern, InvalidBytePatternFailure> TryParse(string patternString)
    {
        var parseResult = ParsePatternString(patternString);
        if (parseResult.IsFailure)
            return parseResult.Error;

        (byte[] byteArray, byte[] mask) = parseResult.Value;
        return new ByteSearchPattern(patternString, byteArray, mask);
    }

    /// <summary>
    /// Parses a pattern string into a byte array and a mask array.
    /// </summary>
    /// <param name="patternString">Pattern string to parse.</param>
    /// <returns>A tuple containing the byte array and the mask array parsed from the pattern string, or an instance of
    /// <see cref="InvalidBytePatternFailure"/> detailing the reason for the failure.</returns>
    private static Result<Tuple<byte[], byte[]>, InvalidBytePatternFailure> ParsePatternString(
        string patternString)
    {
        if (string.IsNullOrWhiteSpace(patternString))
            return new InvalidBytePatternFailure("The pattern cannot be null or empty.");
        
        patternString = patternString.Replace(" ", "");
        if (patternString.Length % 2 != 0)
            return new InvalidBytePatternFailure(
                "The pattern must contain an even number of non-space characters.");
        
        if (!BytePatternRegex.IsMatch(patternString))
            return new InvalidBytePatternFailure(
                "The pattern must contain only hexadecimal characters and '?' wildcards.");
        
        if (patternString == new string('?', patternString.Length))
            return new InvalidBytePatternFailure("The pattern cannot contain only '?' wildcards.");

        var bytePatternArray = new byte[patternString.Length / 2];
        var maskArray = new byte[patternString.Length / 2];
        for (var i = 0; i < patternString.Length; i += 2)
        {
            string byteString = patternString.Substring(i, 2);
            if (byteString[0] == '?' && byteString[1] == '?')
            {
                // Both bytes are unknown. Set both the value and the mask to 0.
                bytePatternArray[i / 2] = 0;
                maskArray[i / 2] = 0;
            }
            else if (byteString[0] == '?')
            {
                // The first byte is unknown. Set the value to the second byte and the mask to 0xF.
                bytePatternArray[i / 2] = Convert.ToByte(byteString[1].ToString(), 16);
                maskArray[i / 2] = 0xF;
            }
            else if (byteString[1] == '?')
            {
                // The second byte is unknown. Set the value to the first byte multiplied by 16 and the mask to 0xF0.
                bytePatternArray[i / 2] = (byte)(Convert.ToByte(byteString[0].ToString(), 16) * 16);
                maskArray[i / 2] = 0xF0;
            }
            else
            {
                // Both bytes are known. Set the value to the byte and the mask to 0xFF.
                bytePatternArray[i / 2] = Convert.ToByte(byteString, 16);
                maskArray[i / 2] = 0xFF;
            }
        }

        return new Tuple<byte[], byte[]>(bytePatternArray, maskArray);
    }
    
    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => _originalPatternString;
    
    /// <summary>
    /// Implicitly converts a string to a <see cref="ByteSearchPattern"/>.
    /// </summary>
    /// <param name="patternString">Pattern string to convert.</param>
    /// <returns>A new instance of <see cref="ByteSearchPattern"/> created from the given pattern string.</returns>
    /// <exception cref="ArgumentException">Thrown when the pattern string is invalid.</exception>
    public static implicit operator ByteSearchPattern(string patternString) => new(patternString);
}
