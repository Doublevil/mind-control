using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

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
    /// Stores data parsed and computed internally from a pointer path expression.
    /// </summary>
    private struct ExpressionParsedData
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
        public BigInteger BaseModuleOffset { get; init; }
        
        /// <summary>
        /// Gets the collection of pointer offsets to follow sequentially in order to evaluate the memory address.
        /// For example, for the expression "myprocess.exe+01F4684-4,18+4,C", gets [0x1C, 0xC].
        /// </summary>
        public BigInteger[] PointerOffsets { get; init; }
        
        /// <summary>
        /// Gets a boolean indicating if the address is a 64-bit only address, or if it can also be used in a 32-bits
        /// process.
        /// For example, for the expression "app.dll+0000F04AA1218410", this boolean would be True.
        /// For the expression "app.dll+00000000F04AA121", this boolean would be False.
        /// Note that evaluating a 32-bit-compatible address may still end up overflowing.
        /// </summary>
        public bool IsStrictly64Bits { get; init; }
    }
    
    /// <summary>
    /// Gets the pointer path expression. Some examples include "myprocess.exe+001F468B,1B,0,A0",
    /// or "1F07A314", or "1F07A314+A0", or "1F07A314-A0,4".
    /// </summary>
    public string Expression { get; }

    private readonly ExpressionParsedData _parsedData;

    /// <summary>
    /// Gets the base module name.
    /// For example, for the expression "myprocess.exe+01F4684-4,18+4,C", gets "myprocess.exe".
    /// For expression with no module name, like "01F4684-4,18", gets a null value.
    /// </summary>
    public string? BaseModuleName => _parsedData.BaseModuleName;

    /// <summary>
    /// Gets the offset of the base module name.
    /// For example, for the expression "myprocess.exe+01F4684-4,18+4,C", gets 0x1F4680.
    /// For expressions without a module offset, like "01F4684-4,18" or "myprocess.exe", gets 0.
    /// </summary>
    public BigInteger BaseModuleOffset => _parsedData.BaseModuleOffset;

    /// <summary>
    /// Gets the collection of pointer offsets to follow sequentially in order to evaluate the memory address.
    /// For example, for the expression "myprocess.exe+01F4684-4,18+4,C", gets [0x1C, 0xC].
    /// </summary>
    public BigInteger[] PointerOffsets => _parsedData.PointerOffsets;

    /// <summary>
    /// Gets a boolean indicating if the address is a 64-bit only address, or if it can also be used in a 32-bits
    /// process.
    /// For example, for the expression "app.dll+0000F04AA1218410", this boolean would be True.
    /// For the expression "app.dll+00000000F04AA121", this boolean would be False.
    /// Note that evaluating a 32-bit-compatible address may still end up overflowing.
    /// </summary>
    public bool IsStrictly64Bits => _parsedData.IsStrictly64Bits;
    
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
    private PointerPath(string expression, ExpressionParsedData? parsedData)
    {
        Expression = expression;
        _parsedData = parsedData ?? throw new ArgumentException(
            $"The provided expression \"{expression}\" is not valid. Please check the expression syntax guide for more information.",
            nameof(expression));
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
    /// <param name="allowOnly32Bits">If set to True, valid 64-bit expressions will still cause False to be returned.
    /// </param>
    /// <returns>True if the expression is valid.</returns>
    public static bool IsValid(string expression, bool allowOnly32Bits = false)
    {
        var parsedData = Parse(expression);
        return parsedData != null && (!allowOnly32Bits || !parsedData.Value.IsStrictly64Bits);
    }

    /// <summary>
    /// Attempts to parse the given expression. Returns the resulting <see cref="PointerPath"/> instance if the
    /// expression was successfully parsed, or null if the expression is not valid.
    /// </summary>
    /// <param name="expression">Expression to check.</param>
    /// <param name="allowOnly32Bits">If set to True, valid 64-bit expressions will still cause null to be returned.
    /// </param>
    /// <returns>The resulting <see cref="PointerPath"/> instance if the expression was successfully parsed, or
    /// null if the expression is not valid.</returns>
    public static PointerPath? TryParse(string expression, bool allowOnly32Bits = false)
    {
        var parsedData = Parse(expression);
        return parsedData == null || allowOnly32Bits && parsedData.Value.IsStrictly64Bits ?
            null : new PointerPath(expression, parsedData);
    }

    /// <summary>
    /// Attempts to parse the given expression. Returns the parsed data container when successful, or null if the
    /// expression is not valid.
    /// </summary>
    /// <param name="expression">Expression to parse.</param>
    /// <returns>the parsed data container when successful, or null if the expression is not valid.</returns>
    private static ExpressionParsedData? Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return null;

        var baseModuleName = ParseBaseModuleName(expression);
        var pointerOffsets = ParsePointerOffsets(expression, baseModuleName != null);
        
        // If pointer offsets cannot be parsed, it means the expression is not valid and so we cannot parse.
        if (pointerOffsets == null)
            return null;
        
        // If there is no base module name, the base address cannot be negative (e.g. "-1F,4").
        if (baseModuleName == null && pointerOffsets.Length > 0 && pointerOffsets[0] < 0)
            return null;
        
        var moduleOffset = baseModuleName != null ? ParseFirstStaticOffset(expression) : 0;
        // If the base module offset expression is invalid, we cannot parse the expression.
        if (moduleOffset == null)
            return null;
        
        bool is64Bits = pointerOffsets.Append(moduleOffset.Value).Any(o => BigInteger.Abs(o) > uint.MaxValue);

        return new ExpressionParsedData
        {
            BaseModuleName = baseModuleName,
            BaseModuleOffset = moduleOffset.Value,
            PointerOffsets = pointerOffsets,
            IsStrictly64Bits = is64Bits
        };
    }

    /// <summary>
    /// Attempts to parse a base module name from the given expression.
    /// For example, in "myapp.exe+1D,28", get "myapp.exe".
    /// If the expression does not contain a base module name (e.g. "1F+6"), returns a null value.
    /// Note that the definition of a base module name is very loose. All characters, including special characters and
    /// numbers, are valid, with the exception of symbols used for other purposes in the pointer path syntax (,+-). 
    /// </summary>
    /// <param name="expression">Target expression to parse.</param>
    /// <returns>The base module name found in the input expression, or null if the expression does not contain a
    /// module name.</returns>
    private static string? ParseBaseModuleName(string expression)
    {
        // No module name in an empty expression.
        if (string.IsNullOrWhiteSpace(expression))
            return null;

        // Take the first part of the first offset by splitting on "," and then on "+" or "-".
        // Some examples:
        // - For "myapp.exe+1D,28", get "myapp.exe"
        // - For "1F284604+1D-4,1D", get "1F284604"
        // - For "-1F+6F044C", get "" (because it starts with a sign). It can't be a module name so it's fine.
        string firstOffset = expression.Split(',').First();
        string potentialModuleName = firstOffset.Split('-', '+').First();

        // If the potential module name is empty, return null.
        if (string.IsNullOrWhiteSpace(potentialModuleName))
            return null;
        
        // Now we need to determine whether our potential module name is a real module name, or an offset/address.
        // Attempt to parse it as a ulong. If it can't be parsed, it's a module name.
        if (ulong.TryParse(potentialModuleName, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
            return null;
        
        // If we are here, we do have a module name. Format it and return it.
        return potentialModuleName.Trim().Trim('"');
    }
    
    /// <summary>
    /// In the given expression, retrieves the static offset value to add up with the first part of the first pointer
    /// offset. This method is intended to parse the static offset of a module name.
    /// For example, for the expression "myprocess.exe+1C-4,1B", the result would be 0x18 (0x1C-0x4 = 0x18).
    /// If the expression contains no offset after the first part, the result will be 0.
    /// If the offset expression is not valid, returns null.
    /// </summary>
    private static BigInteger? ParseFirstStaticOffset(string expression)
    {
        var firstSignOperatorIndex = expression.IndexOfAny(new[] { '+', '-' }, 1);
        var firstPointerOperatorIndex = expression.IndexOf(',');
        
        // If there are no pointer operators in the expression, adjust the index to the length of the expression
        if (firstPointerOperatorIndex == -1)
            firstPointerOperatorIndex = expression.Length;

        // No sign operator: no offset
        if (firstSignOperatorIndex < 0)
            return 0;
        
        // First sign operator is after the first pointer operator (e.g. "myprocess.exe,1C+4"): no offset
        if (firstSignOperatorIndex > firstPointerOperatorIndex)
            return 0;
        
        // Parse whatever is between the first sign operator and the first pointer operator
        string offsetExpression = expression.Substring(firstSignOperatorIndex,
            firstPointerOperatorIndex - firstSignOperatorIndex);
        return ParsePointerOffsetExpression(offsetExpression);
    }
    
    /// <summary>
    /// In the given expression, retrieves and evaluates all pointer expressions (sequences of offsets separated by a
    /// comma). For example, "18+4,C" would yield [0x1C, 0xC].
    /// </summary>
    /// <param name="expression">Expression to parse.</param>
    /// <param name="hasModuleName">If True, the first pointer offset will be ignored because it contains a module name
    /// and thus cannot be evaluated into a number. For example, "myapp.exe+8F,1C", would only return [0x1C].</param>
    /// <returns>Pointer offsets evaluated as BigIntegers.</returns>
    private static BigInteger[]? ParsePointerOffsets(string expression, bool hasModuleName)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return null;
        
        var offsetExpressions = expression.Split(',');
        
        var results = new List<BigInteger>();
        // Skip the first split if the expression is known to have a module name.
        foreach (var offsetExpression in offsetExpressions.Skip(hasModuleName ? 1 : 0))
        {
            // Parse each expression, and if any fails to parse, it means that the expression is not valid, and so
            // we return null instead of the expected array.
            var parsedOffset = ParsePointerOffsetExpression(offsetExpression);
            if (parsedOffset == null)
                return null;

            results.Add(parsedOffset.Value);
        }

        return results.ToArray();
    }

    /// <summary>
    /// Tries to parse the given pointer offset expression and returns the resulting value.
    /// Because valid results range between -FFFFFFFFFFFFFFFF and FFFFFFFFFFFFFFFF, the return value is a BigInteger.
    /// If the expression is invalid or results in a number that is outside of the valid range, returns null.
    /// </summary>
    /// <param name="pointerOffsetExpression">Expression to parse. Example: "1F06+7C-4".</param>
    /// <returns>A BigInteger between -FFFFFFFFFFFFFFFF and FFFFFFFFFFFFFFFF representing the offset computed
    /// from the expression, or null when the expression is invalid or the result is out of range.</returns>
    private static BigInteger? ParsePointerOffsetExpression(string pointerOffsetExpression)
    {
        // Use a regular expression to find all of the offset numbers to add up.
        // The regex translates to:
        // An optional + or - sign, followed by a hex number, optionally followed by any number of expressions comprised
        // of a mandatory + or - sign followed by a hex number.
        // The ^ and $ at the start and the end make sure that it won't match expressions that start or end with
        // anything unexpected.
        var offsetRegex = new Regex("^([+-]?[0-9a-f]{1,16})([+-][0-9a-f]{1,16})*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        
        // Replace all whitespaces in the expression to support a variety of cases.
        // Examples: "1F 06 + 7C - 8" would parse the same as "1F06+7C-8", which seems like a good idea.
        var match = offsetRegex.Match(pointerOffsetExpression.Replace(" ", string.Empty));

        // If the regex doesn't match, the expression is invalid.
        if (!match.Success)
            return null;

        var offsets = match.Groups[2].Captures.Select(c => c.Value).Append(match.Groups[1].Value);
        
        // Add up all the parts using BigIntegers.
        // The regex has a 16-length limit for individual offsets, but we don't really care to check out of bounds
        // values until everything is added up. For instance, we could have FFFFFFFFFFFFFFFF+1-1, which would be
        // valid and result in a final value of FFFFFFFFFFFFFFFF.
        BigInteger sum = 0;
        foreach (var offset in offsets)
        {
            // An empty offset means the whole pointer offset expression is invalid.
            if (string.IsNullOrWhiteSpace(offset))
                return null;

            // Negative hex numbers cannot be parsed natively in .net, so we need to handle them manually
            bool isNegative = offset[0] == '-';
            string parsableOffset = isNegative || offset[0] == '+' ? offset[1..] : offset;
            
            if (!ulong.TryParse(parsableOffset, NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                    out var offsetValue))
            {
                // The offset cannot be parsed, therefore the whole pointer offset is invalid.
                return null;
            }

            sum = isNegative ? sum - offsetValue : sum + offsetValue;
        }

        // With the final sum, check if the result is in the valid range for 64-bit addresses, positive or negative.
        if (sum > ulong.MaxValue || sum < BigInteger.Negate(ulong.MaxValue))
            return null;
        
        return sum;
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return Expression;
    }
}