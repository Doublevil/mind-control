namespace MindControl;

/// <summary>
/// Holds a string expression consisting in a base address followed by a sequence of pointer offsets.
/// Allows programs to consistently retrieve data in the process memory by following pointers to reach a dynamic
/// address.
/// A pointer path instance can be reused to optimize performance, as it is only parsed once, when constructed.
/// </summary>
public class PointerPath
{
    /// <summary>
    /// Stores the properties of a pointer path expression that are used in computations.
    /// </summary>
    private readonly struct ExpressionInternalData
    {
        /// <summary>
        /// Gets the base module name.
        /// For example, for the expression "myprocess.exe+01F4684-4,18+4,C", gets "myprocess.exe".
        /// For expression with no module name, like "01F4684-4,18", gets a null value.
        /// </summary>
        public string? BaseModuleName { get; init; }
        
        /// <summary>
        /// Gets the offset of the base module name.
        /// For example, for the expression "myprocess.exe+01F4684-4,18+4,C", gets 0x1F4680.
        /// For expressions without a module offset, like "01F4684-4,18" or "myprocess.exe", gets 0.
        /// </summary>
        public PointerOffset BaseModuleOffset { get; init; }
        
        /// <summary>
        /// Gets the collection of pointer offsets to follow sequentially in order to evaluate the memory address.
        /// For example, for the expression "myprocess.exe+01F4684-4,18+4,C", gets [0x1C, 0xC].
        /// </summary>
        public PointerOffset[] PointerOffsets { get; init; }
        
        /// <summary>
        /// Gets a boolean indicating if the path is a 64-bit only path, or if it can also be used in a 32-bit
        /// process.
        /// </summary>
        public bool IsStrictly64Bit => BaseModuleOffset.Is64Bit || PointerOffsets.Any(offset => offset.Is64Bit);
    }

    private string? _expression;
    
    /// <summary>
    /// Gets the pointer path expression. Some examples include "myprocess.exe+001F468B,1B,0,A0",
    /// or "1F07A314", or "1F07A314+A0", or "1F07A314-A0,4".
    /// </summary>
    public string Expression => _expression ??= BuildExpression();

    private readonly ExpressionInternalData _internalData;

    /// <summary>
    /// Gets the base module name.
    /// For example, for the expression "myprocess.exe+01F4684-4,18+4,C", gets "myprocess.exe".
    /// For expression with no module name, like "01F4684-4,18", gets a null value.
    /// </summary>
    public string? BaseModuleName => _internalData.BaseModuleName;

    /// <summary>
    /// Gets the offset of the base module name.
    /// For example, for the expression "myprocess.exe+01F4684-4,18+4,C", gets 0x1F4680.
    /// For expressions without a module offset, like "01F4684-4,18" or "myprocess.exe", gets 0.
    /// </summary>
    public PointerOffset BaseModuleOffset => _internalData.BaseModuleOffset;

    /// <summary>
    /// Gets the collection of pointer offsets to follow sequentially in order to evaluate the memory address.
    /// For example, for the expression "myprocess.exe+01F4684-4,18+4,C", gets [0x1C, 0xC].
    /// </summary>
    public PointerOffset[] PointerOffsets => _internalData.PointerOffsets;

    /// <summary>
    /// Gets a boolean indicating if the path is a 64-bit only path, or if it can also be used in a 32-bit
    /// process.
    /// For example, for the expression "app.dll+0000F04AA1218410", this boolean would be True.
    /// For the expression "app.dll+00000000F04AA121", this boolean would be False.
    /// Note that evaluating a 32-bit-compatible address may still end up overflowing.
    /// </summary>
    public bool IsStrictly64Bit => _internalData.IsStrictly64Bit;
    
    /// <summary>
    /// Builds a pointer path from the given expression.
    /// </summary>
    /// <param name="expression">Pointer path expression. Some examples include "myprocess.exe+001F468B,1B,0,A0",
    /// or "1F07A314", or "1F07A314+A0", or "1F07A314-A0,4".</param>
    /// <exception cref="ArgumentException">Thrown when the expression is not valid.</exception>
    public PointerPath(string expression) : this(expression, Parse(expression)) {}
    
    /// <summary>
    /// Builds a pointer path from the given expression and parsed data.
    /// </summary>
    /// <param name="expression">Pointer path expression. Some examples include "myprocess.exe+001F468B,1B,0,A0",
    /// or "1F07A314", or "1F07A314+A0", or "1F07A314-A0,4".</param>
    /// <param name="parsedData">An instance containing data that was already parsed from the expression.</param>
    /// <exception cref="ArgumentException">Thrown when the expression is not valid.</exception>
    private PointerPath(string expression, ExpressionInternalData? parsedData)
    {
        _expression = expression;
        _internalData = parsedData ?? throw new ArgumentException(
            $"The provided expression \"{expression}\" is not valid. Please check the expression syntax guide for more information.",
            nameof(expression));
    }

    /// <summary>
    /// Builds a pointer path from a pointer and a series of offsets.
    /// </summary>
    /// <param name="basePointerAddress">Address of the base pointer, which is the first address to evaluate.
    /// For example, in "1F07A314,4,1C", this would be 0x1F07A314.</param>
    /// <param name="pointerOffsets">Collection of offsets to follow sequentially in order to evaluate the
    /// final memory address. For example, in "1F07A314,4,1C", this would be [0x4, 0x1C].</param>
    public PointerPath(UIntPtr basePointerAddress, params long[] pointerOffsets)
    {
        _internalData = new ExpressionInternalData
        {
            BaseModuleName = null,
            BaseModuleOffset = PointerOffset.Zero,
            PointerOffsets = new[] { new PointerOffset(basePointerAddress.ToUInt64(), false) }
                .Concat(pointerOffsets.Select(o => new PointerOffset((ulong)Math.Abs(o), o < 0))).ToArray()
        };
    }
    
    /// <summary>
    /// Builds a pointer path from a base module name, a base module offset, and a series of offsets.
    /// </summary>
    /// <param name="baseModuleName">Name of the base module, where the starting pointer is found. For example, in
    /// "mygame.exe+3FF0,4,1C", this would be "mygame.exe" (without the quotes).</param>
    /// <param name="baseModuleOffset">Offset applied to the base module to get the address of the first pointer to
    /// evaluate. For example, in "mygame.exe+3FF0,4,1C", this would be 0x3FF0.</param>
    /// <param name="pointerOffsets">Collection of offsets to follow sequentially in order to evaluate the
    /// final memory address. For example, in "mygame.exe+3FF0,4,1C", this would be [0x4, 0x1C].</param>
    public PointerPath(string baseModuleName, UIntPtr baseModuleOffset, params long[] pointerOffsets)
    {
        _internalData = new ExpressionInternalData
        {
            BaseModuleName = baseModuleName,
            BaseModuleOffset = new PointerOffset(baseModuleOffset.ToUInt64(), false),
            PointerOffsets = pointerOffsets.Select(o => new PointerOffset((ulong)Math.Abs(o), o < 0)).ToArray()
        };
    }

    /// <summary>
    /// Implicitly converts the given string to a <see cref="PointerPath"/> instance using the constructor.
    /// </summary>
    /// <param name="s">String to convert.</param>
    /// <returns>Pointer path instance built from the string.</returns>
    public static implicit operator PointerPath(string s) => new(s);
    
    /// <summary>
    /// Checks the given expression and returns a boolean indicating if it is valid or not.
    /// </summary>
    /// <param name="expression">Expression to check.</param>
    /// <param name="allowOnly32Bit">If set to True, valid 64-bit expressions will still cause False to be returned.
    /// </param>
    /// <returns>True if the expression is valid.</returns>
    public static bool IsValid(string expression, bool allowOnly32Bit = false)
    {
        var parsedData = Parse(expression);
        return parsedData != null && (!allowOnly32Bit || !parsedData.Value.IsStrictly64Bit);
    }

    /// <summary>
    /// Attempts to parse the given expression. Returns the resulting <see cref="PointerPath"/> instance if the
    /// expression was successfully parsed, or null if the expression is not valid.
    /// </summary>
    /// <param name="expression">Expression to check.</param>
    /// <param name="allowOnly32Bit">If set to True, valid 64-bit expressions will still cause null to be returned.
    /// </param>
    /// <returns>The resulting <see cref="PointerPath"/> instance if the expression was successfully parsed, or
    /// null if the expression is not valid.</returns>
    public static PointerPath? TryParse(string expression, bool allowOnly32Bit = false)
    {
        var parsedData = Parse(expression);
        return parsedData == null || allowOnly32Bit && parsedData.Value.IsStrictly64Bit ?
            null : new PointerPath(expression, parsedData);
    }
    
    /// <summary>
    /// Attempts to parse the given expression. Returns the parsed data container when successful, or null if the
    /// expression is not valid.
    /// </summary>
    /// <param name="expression">Expression to parse.</param>
    /// <returns>the parsed data container when successful, or null if the expression is not valid.</returns>
    private static ExpressionInternalData? Parse(string expression)
    {
        // A note about the parsing code:
        // It is designed to be fast and to allocate the strict minimum amount of memory.
        // This means we cannot use regular expressions, splits, trims, lists, etc.
        // That makes the code significantly harder to read and maintain, but this is a potential hot path and so it is
        // worth the compromise.
        
        // Quick checks to avoid parsing the expression if it is obviously invalid
        // The expression must not be empty, and must not end with a comma or an operator
        if (expression.Length == 0 || expression[^1] is ',' or '+' or '-')
            return null;
        
        // Try to read a module name from the expression
        // If the value is null, the expression is invalid and we can straight up return null
        // If there is no module name, the value will be an empty string
        string? baseModuleName = ParseModuleName(expression, out int readCount);
        if (baseModuleName == null)
            return null;
        bool hasModule = baseModuleName.Length > 0;

        // If the whole expression was the module name, we can return the parsed data
        if (hasModule && readCount == expression.Length)
        {
            return new ExpressionInternalData
            {
                BaseModuleName = baseModuleName,
                BaseModuleOffset = PointerOffset.Zero,
                PointerOffsets = []
            };
        }

        // Now we need to know if the module name has an offset (e.g. the "+4" in "myprocess.exe+4,2B")
        // If the next character after the module name is a comma, it doesn't have an offset
        bool hasModuleOffset = hasModule && expression[readCount] != ',';
        if (hasModule && !hasModuleOffset)
            readCount++;

        // Then, we determine how many offsets there are in the expression
        // This is done by counting the number of commas in the expression
        // The reason we need to know in advance is to allocate the array of offsets, to avoid the cost of a List
        int offsetCount = hasModuleOffset ? 0 : 1;
        for (int i = readCount; i < expression.Length; i++)
        {
            if (expression[i] == ',')
                offsetCount++;
        }

        var offsets = new PointerOffset[offsetCount];
        var index = readCount;
        PointerOffset? baseModuleOffset = null;
        int offsetIndex = 0;
        while (index < expression.Length)
        {
            var offset = ParsePointerOffsetExpression(expression, index, out readCount);
            
            // If there is no base module, the first offset cannot be negative (we cannot start at a negative address)
            if (offset == null || !hasModule && offsetIndex == 0 && offset.Value.IsNegative)
                return null;

            // If there is a base module offset, the first offset is the base module offset
            if (hasModuleOffset && baseModuleOffset == null)
                baseModuleOffset = offset;
            else
                offsets[offsetIndex++] = offset.Value;
            
            // Advance to the next expression
            index += readCount;
        }
        
        return new ExpressionInternalData
        {
            BaseModuleName = hasModule ? baseModuleName : null,
            BaseModuleOffset = baseModuleOffset ?? PointerOffset.Zero,
            PointerOffsets = offsets
        };
    }

    /// <summary>
    /// Parses the module name from the given expression.
    /// </summary>
    /// <param name="expression">Expression to parse.</param>
    /// <param name="readCount">Number of characters read from the expression.</param>
    /// <returns>The module name, or null if the expression is invalid.</returns>
    private static string? ParseModuleName(string expression, out int readCount)
    {
        readCount = 0;
        if (expression.Length == 0)
            return null;

        int startIndex = 0;
        // Skip all whitespaces at the start of the expression
        while (startIndex < expression.Length && char.IsWhiteSpace(expression[startIndex]))
            startIndex++;
        
        if (startIndex == expression.Length)
            return null; // All whitespace

        // Determine if the module name is quoted.
        // The rule is that the module name can optionally be quoted with double-quotes.
        // In this case, the returned module name will be the content inside the quotes.
        if (expression[startIndex] == '"')
        {
            // After skipping spaces, the first character is a quote
            // This means the module name is quoted. Advance the start index to the first character after the quote.
            startIndex++;
            int endQuoteIndex = expression.IndexOf('"', startIndex);
            if (endQuoteIndex == -1 || endQuoteIndex == startIndex + 1)
                return null; // No closing quote or nothing inside the quotes: the expression is invalid
            
            readCount = endQuoteIndex + 1;
            return expression.Substring(startIndex, endQuoteIndex - startIndex);
        }

        // The module name is not quoted. If there is a module name, it will end either with a +/- operator, a ',', or
        // the end of the string.
        int endIndex = expression.IndexOfAny(['-', '+', ',']);
        if (endIndex == -1)
            endIndex = expression.Length;

        // Remove all white spaces at the end of the module name
        while (endIndex > startIndex && char.IsWhiteSpace(expression[endIndex - 1]))
            endIndex--;
        
        // If the endIndex was reduced to the startIndex, there is no module name
        if (endIndex == startIndex)
            return string.Empty;
        
        int length = endIndex - startIndex;
        
        // We have to check if the module can be a valid hexadecimal address.
        // If it is, what we read is an address and not a module, so we must return string.Empty.
        PointerOffset? currentValue = PointerOffset.Zero;
        for (int i = startIndex; i < endIndex; i++)
        {
            char c = expression[i];
            if (char.IsWhiteSpace(c))
                continue;

            byte hexadecimalValue = CharToValue(c);
            if (hexadecimalValue == 255)
            {
                // Non-hexadecimal character
                readCount = endIndex;
                return expression.Substring(startIndex, length);
            }
            
            currentValue = currentValue.Value.ShiftAndAdd(hexadecimalValue);
            if (currentValue == null)
            {
                // Larger than the largest possible address
                readCount = endIndex;
                return expression.Substring(startIndex, length);
            }
        }

        return string.Empty; // The module name is a valid hexadecimal address
    }
    
    /// <summary>
    /// Parses a pointer offset expression (a sub-section that comes in-between ',' characters) from the given
    /// expression.
    /// </summary>
    /// <param name="expression">Full expression to parse.</param>
    /// <param name="startIndex">Index to start parsing from.</param>
    /// <param name="readCount">Number of characters read from the expression.</param>
    /// <returns>The parsed pointer offset, or null if the expression is invalid.</returns>
    private static PointerOffset? ParsePointerOffsetExpression(string expression, int startIndex,
        out int readCount)
    {
        readCount = 0;
        
        PointerOffset? sum = PointerOffset.Zero;
        PointerOffset? currentNumber = PointerOffset.Zero;
        bool lastCharWasOperator = false;
        bool hasMeaningfulCharacters = false;
        
        for (int i = startIndex; i < expression.Length; i++)
        {
            readCount++;
            
            char c = expression[i];
            
            // Ignore all spaces everywhere in the expression
            if (char.IsWhiteSpace(c))
                continue;

            if (c is '+' or '-')
            {
                if (lastCharWasOperator)
                    return null; // Two operators in a row is invalid

                // We just finished reading a number, or start reading a new one.
                // We must add the current number to the sum, and reset the current number.
                sum = sum.Value.Plus(currentNumber.Value);
                if (sum == null)
                    return null; // Overflow or underflow
                currentNumber = new PointerOffset(0, IsNegative: c == '-');
                
                // Set the operator for the next number
                lastCharWasOperator = true;
                continue;
            }

            if (c is ',')
                break; // The sub-expression parsed by this method stops here
            
            // From here on, we are reading a non-operator character.
            // This could be a number or an invalid character.
            lastCharWasOperator = false;
            byte value = CharToValue(c);
            if (value == 255)
                return null; // Invalid character
            
            hasMeaningfulCharacters = true;
            currentNumber = currentNumber.Value.ShiftAndAdd(value);
            if (currentNumber == null)
                return null; // Overflow or underflow
        }

        // We are done parsing the sub-expression.
        // If it didn't contain any meaningful character, or ended with an operator, it is invalid.
        if (lastCharWasOperator || !hasMeaningfulCharacters)
            return null;
        
        // Return the final sum. If it overflows or underflows, this will return null, as the expression is invalid.
        return sum.Value.Plus(currentNumber.Value);
    }
    
    /// <summary>
    /// Converts a character to its hexadecimal value.
    /// </summary>
    /// <param name="c">Character to convert.</param>
    /// <returns>Hexadecimal value of the character, or 255 if the character is not a valid hexadecimal
    /// character.</returns>
    private static byte CharToValue(char c)
    {
        return c switch
        {
            >= '0' and <= '9' => (byte)(c - '0'),
            >= 'A' and <= 'F' => (byte)(c - 'A' + 10),
            >= 'a' and <= 'f' => (byte)(c - 'a' + 10),
            _ => 255
        };
    }
    
    /// <summary>Builds the expression string from the properties. Used when the pointer path is built from pointers and
    /// not parsed from a string expression.</summary>
    private string BuildExpression()
    {
        var offsetString = string.Join(',', PointerOffsets.Select(o => o.ToString()));
        return BaseModuleName != null ? $"\"{BaseModuleName}\"+{BaseModuleOffset},{offsetString}" : offsetString;
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => Expression;
}