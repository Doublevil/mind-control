using System.Text;

namespace MindControl;

/// <summary>
/// Defines how strings are read and written in a process' memory.
/// </summary>
public class StringSettings
{
    #region Presets
    
    /// <summary>
    /// Default string settings preset (null-terminated UTF-16 without length prefix). This may not work well for your
    /// process. Please use different settings if you notice corrupt characters in your strings.
    /// </summary>
    public static readonly StringSettings Default = new(Encoding.Unicode, true, null);

    /// <summary>
    /// An alternative to the default string settings preset using the equally common UTF-8 instead of UTF-16. Try this
    /// preset if the default one does not work for your process.
    /// Please try different settings if you notice corrupt characters in your strings.
    /// </summary>
    public static readonly StringSettings DefaultUtf8 = new(Encoding.UTF8, true, null);
    
    /// <summary>
    /// String settings preset for .net processes (UTF-16 with both length prefix and null terminator). Be aware that a
    /// process may hold strings that use different settings.
    /// </summary>
    public static readonly StringSettings DotNet = new(Encoding.Unicode, true, new StringLengthPrefixSettings(4, 2));

    /// <summary>
    /// String setting preset for Java processes (UTF-16 with length prefix, no null terminator). Be aware that a
    /// process may hold strings that use different settings. In particular, under some circumstances, Java may use the
    /// ISO-8859-1 encoding for some strings to optimize performance.
    /// </summary>
    public static readonly StringSettings Java = new(Encoding.Unicode, false, new StringLengthPrefixSettings(4, 2));

    /// <summary>
    /// String setting preset for Rust processes (UTF-8 with length prefix, no null terminator). Be aware that a
    /// process may hold strings that use different settings.
    /// </summary>
    public static readonly StringSettings Rust = new(Encoding.UTF8, false, new StringLengthPrefixSettings(2, 1));
    
    #endregion
    
    /// <summary>
    /// Gets or sets a boolean indicating if strings should have a \0 delimitation character at the end.
    /// </summary>
    public bool IsNullTerminated { get; set; }
    
    /// <summary>
    /// Gets or sets the encoding of the strings.
    /// </summary>
    public Encoding Encoding { get; set; }
    
    /// <summary>
    /// Gets or sets the length prefix settings.
    /// If null, strings are considered to have no length prefix.
    /// </summary>
    public StringLengthPrefixSettings? PrefixSettings { get; set; }

    /// <summary>
    /// Builds settings with the given properties.
    /// </summary>
    /// <param name="encoding">Encoding of the strings.</param>
    /// <param name="isNullTerminated">Boolean indicating if strings should have a \0 delimitation character at the end.
    /// </param>
    /// <param name="prefixSettings">Length prefix settings. If null, strings are considered to have no length prefix.
    /// </param>
    public StringSettings(Encoding encoding, bool isNullTerminated, StringLengthPrefixSettings? prefixSettings)
    {
        Encoding = encoding;
        IsNullTerminated = isNullTerminated;
        PrefixSettings = prefixSettings;
    }
    
    /// <summary>
    /// Builds settings with the given properties. With this constructor, strings are considered to have no length
    /// prefix. 
    /// </summary>
    /// <param name="encoding">Encoding of the strings.</param>
    /// <param name="isNullTerminated">Boolean indicating if strings should have a \0 delimitation character at the end.
    /// </param>
    public StringSettings(Encoding encoding, bool isNullTerminated) : this(encoding, isNullTerminated, null) {}

    /// <summary>
    /// Builds settings with the given properties. With this constructor, strings are considered to have no length
    /// prefix, and to be null-terminated.
    /// </summary>
    /// <param name="encoding">Encoding of the strings.</param>
    public StringSettings(Encoding encoding) : this(encoding, true, null) {}
}

/// <summary>
/// Defines settings about the prefix that holds the length of a string.
/// </summary>
public class StringLengthPrefixSettings
{
    /// <summary>
    /// Gets or sets the number of bytes to read or write as a length prefix before strings.
    /// </summary>
    public int PrefixSize { get; set; }
    
    /// <summary>
    /// Gets or sets the number of bytes counted by the length prefix.
    /// For example, in a read operation, if this value is 2 and the length prefix evaluates to 21, the string will be
    /// read as 42 bytes.
    /// If null, this value will be automatically determined using the encoding.
    /// </summary>
    public int? LengthUnit { get; set; }

    /// <summary>
    /// Builds length prefix settings with the given properties.
    /// </summary>
    /// <param name="prefixSize">Number of bytes to read or write as a length prefix before strings.</param>
    /// <param name="lengthUnit">Number of bytes counted by the length prefix. Automatically determined if set to null.
    /// </param>
    public StringLengthPrefixSettings(int prefixSize, int? lengthUnit)
    {
        if (prefixSize is > 8 or < 1)
            throw new ArgumentOutOfRangeException(nameof(prefixSize), "Prefix size must be between 1 and 8 included.");
        
        PrefixSize = prefixSize;
        LengthUnit = lengthUnit;
    }
    
    /// <summary>
    /// Builds length prefix settings with the given properties.
    /// With this constructor, the length unit is set to be determined automatically.
    /// </summary>
    /// <param name="prefixSize">Number of bytes to read or write as a length prefix before strings.</param>
    public StringLengthPrefixSettings(int prefixSize) : this(prefixSize, null) {}
}
