using NUnit.Framework;

namespace MindControl.Test.AddressingTests;

/// <summary>
/// Tests the <see cref="PointerPath"/> class.
/// </summary>
public class PointerPathTest
{
    public readonly struct ExpressionTestCase
    {
        public string Expression { get; init; }
        public bool ShouldBeValid { get; init; }
        public string? ExpectedModuleName { get; init; }
        public PointerOffset ExpectedModuleOffset { get; init; }
        public PointerOffset[]? ExpectedPointerOffsets { get; init; }
        public bool Expect64BitOnly { get; init; }
        public string Explanation { get; init; }
        public override string ToString() => Expression;
    }

    /// <summary>
    /// This list describes all test cases and their expected detailed parsing result.
    /// If you need to adjust the format of the pointer path expressions, start by adding your examples here.
    /// </summary>
    private static List<ExpressionTestCase> _testCases = new()
    {
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe+1F016644,13,A0,0",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.exe",
            ExpectedModuleOffset = new(0x1F016644, false),
            ExpectedPointerOffsets = new[] { new(0x13, false), new(0xA0, false), PointerOffset.Zero },
            Explanation = "Expressions must support a base module name, static offsets (+/-) and pointer offsets (,)."
        },
        new ExpressionTestCase
        {
            Expression = "  mymoduleName.exe  +  1F016644 ,  13   ,A0 ,0   ",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.exe",
            ExpectedModuleOffset = new(0x1F016644, false),
            ExpectedPointerOffsets = new[] { new(0x13, false), new(0xA0, false), PointerOffset.Zero },
            Explanation = "Arbitrary whitespaces before and after any subexpression must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "\"mymoduleName.exe\"+1F01664D",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.exe",
            ExpectedModuleOffset = new(0x1F01664D, false),
            ExpectedPointerOffsets = Array.Empty<PointerOffset>(),
            Explanation = "Module names with double-quotes must be supported, and interpreted without double-quotes."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.anything+1F01664D",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.anything",
            ExpectedModuleOffset = new(0x1F01664D, false),
            ExpectedPointerOffsets = Array.Empty<PointerOffset>(),
            Explanation = "Module names with any extension must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName+1F01664D",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName",
            ExpectedModuleOffset = new(0x1F01664D, false),
            ExpectedPointerOffsets = Array.Empty<PointerOffset>(),
            Explanation = "Module names without an extension must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe,0F",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.exe",
            ExpectedModuleOffset = PointerOffset.Zero,
            ExpectedPointerOffsets = new PointerOffset[] { new(0x0F, false) },
            Explanation = "Module names with no static offsets must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe-0F",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.exe",
            ExpectedModuleOffset = new(0x0F, true),
            ExpectedPointerOffsets = Array.Empty<PointerOffset>(),
            Explanation = "Module names with a negative static offset must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.exe",
            ExpectedModuleOffset = PointerOffset.Zero,
            ExpectedPointerOffsets = Array.Empty<PointerOffset>(),
            Explanation = "Module names on their own must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "my1FmoduleName.exe+1F",
            ShouldBeValid = true,
            ExpectedModuleName = "my1FmoduleName.exe",
            ExpectedModuleOffset = new(0x1F, false),
            ExpectedPointerOffsets = Array.Empty<PointerOffset>(),
            Explanation = "Module names containing numerals must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "1FmymoduleName.exe+1F",
            ShouldBeValid = true,
            ExpectedModuleName = "1FmymoduleName.exe",
            ExpectedModuleOffset = new(0x1F, false),
            ExpectedPointerOffsets = Array.Empty<PointerOffset>(),
            Explanation = "Module names starting with numerals must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "1F016644,13,A0,0",
            ShouldBeValid = true,
            ExpectedModuleName = null,
            ExpectedModuleOffset = PointerOffset.Zero,
            ExpectedPointerOffsets = new[] { new(0x1F016644, false), new(0x13, false),
                new(0xA0, false), PointerOffset.Zero },
            Explanation = "Expressions without a module name must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "AF016644",
            ShouldBeValid = true,
            ExpectedModuleName = null,
            ExpectedModuleOffset = PointerOffset.Zero,
            ExpectedPointerOffsets = new PointerOffset[] { new(0xAF016644, false) },
            Explanation = "A static address by itself must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe+4,-2F",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.exe",
            ExpectedModuleOffset = new(0x4, false),
            ExpectedPointerOffsets = new PointerOffset[] { new(0x2F, true) },
            Explanation = "Negative pointer offsets must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe+6A-2C+8",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.exe",
            ExpectedModuleOffset = new(0x46, false),
            ExpectedPointerOffsets = Array.Empty<PointerOffset>(),
            Explanation = "Several static offsets added together must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe,6A-2C+8",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.exe",
            ExpectedModuleOffset = PointerOffset.Zero,
            ExpectedPointerOffsets = new PointerOffset[] { new(0x46, false) },
            Explanation = "Static offsets added together within a pointer offset must be supported."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe+FFFFFFFF,FFFFFFFF",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.exe",
            ExpectedModuleOffset = new(0xFFFFFFFF, false),
            ExpectedPointerOffsets = new PointerOffset[] { new(0xFFFFFFFF, false) },
            Expect64BitOnly = false,
            Explanation = "Offsets within the 32-bit addressing boundaries must be supported in all cases."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe+FFFFFFFFFFFFFFFF,FFFFFFFFFFFFFFFF",
            ShouldBeValid = true,
            ExpectedModuleName = "mymoduleName.exe",
            ExpectedModuleOffset = new(0xFFFFFFFFFFFFFFFF, false),
            ExpectedPointerOffsets = new PointerOffset[] { new(0xFFFFFFFFFFFFFFFF, false) },
            Expect64BitOnly = true,
            Explanation = "Offsets within the 64-bit addressing boundaries must be supported in 64-bit only mode."
        },
        
        // Non valid expression cases
        new ExpressionTestCase
        {
            Expression = string.Empty,
            ShouldBeValid = false,
            Explanation = "An empty expression is invalid."
        },
        new ExpressionTestCase
        {
            Expression = "    ",
            ShouldBeValid = false,
            Explanation = "An all-whitespace expression is invalid."
        },
        new ExpressionTestCase
        {
            Expression = "\"mymodulename.exe+1F016644,13,A0,0",
            ShouldBeValid = false,
            Explanation = "Double-quoted module names must be closed."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe+FFFFFFFFFFFFFFFF+1-1,FFFFFFFFFFFFFFFF+1-1",
            ShouldBeValid = false,
            Explanation = "Offsets sub-summing up to over the 64-bit addressing boundaries must be invalid, even if the sum as a whole is within the boundaries."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe++1F016644",
            ShouldBeValid = false,
            Explanation = "Chaining + symbols is invalid."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe--1F016644",
            ShouldBeValid = false,
            Explanation = "Chaining - symbols is invalid."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe+-1F016644",
            ShouldBeValid = false,
            Explanation = "Chaining + and - symbols is invalid."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe-+1F016644",
            ShouldBeValid = false,
            Explanation = "Chaining - and + symbols is invalid."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe,,1F016644",
            ShouldBeValid = false,
            Explanation = "Chaining , symbols is invalid."
        },
        new ExpressionTestCase
        {
            Expression = "1F016644,",
            ShouldBeValid = false,
            Explanation = "An expression cannot end with a , symbol."
        },
        new ExpressionTestCase
        {
            Expression = "1F016644+",
            ShouldBeValid = false,
            Explanation = "An expression cannot end with a + symbol."
        },
        new ExpressionTestCase
        {
            Expression = "1F016644-",
            ShouldBeValid = false,
            Explanation = "An expression cannot end with a - symbol."
        },
        new ExpressionTestCase
        {
            Expression = "-A0",
            ShouldBeValid = false,
            Explanation = "A static address by itself cannot be negative."
        },
        new ExpressionTestCase
        {
            Expression = "8-A0",
            ShouldBeValid = false,
            Explanation = "A static address by itself cannot be negative (after adding up offsets)."
        },
        new ExpressionTestCase
        {
            Expression = "-A0,8",
            ShouldBeValid = false,
            Explanation = "A static address by itself cannot be negative, even with other pointer offsets."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe+FFFFFFFFFFFFFFFFF",
            ShouldBeValid = false,
            Explanation = "A static offset cannot be over the 64-bit addressing limit."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe+FFFFFFFFFFFFFFFF+1",
            ShouldBeValid = false,
            Explanation = "A static offset cannot be over the 64-bit addressing limit after adding up."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe+anothermodule.dll",
            ShouldBeValid = false,
            Explanation = "A module name cannot be used as a static offset."
        },
        new ExpressionTestCase
        {
            Expression = "mymoduleName.exe,anothermodule.dll",
            ShouldBeValid = false,
            Explanation = "A module name cannot be used as a pointer offset."
        },
        new ExpressionTestCase
        {
            Expression = "1D+anothermodule.dll",
            ShouldBeValid = false,
            Explanation = "An expression with a module name must start with the module name."
        }
    };
    
    /// <summary>
    /// Tests <see cref="PointerPath.IsValid(string,bool)"/> for the given expression test case.
    /// The allowOnly32Bit boolean parameter will be set to False.
    /// Verifies that the result is the expected one.
    /// </summary>
    /// <param name="testCase">Target test case.</param>
    [TestCaseSource(nameof(_testCases))]
    public void IsValidOn64BitTest(ExpressionTestCase testCase)
    {
        bool result = PointerPath.IsValid(testCase.Expression);
        Assert.That(result, Is.EqualTo(testCase.ShouldBeValid), testCase.Explanation);
    }
    
    /// <summary>
    /// Tests <see cref="PointerPath.IsValid(string,bool)"/> for the given expression test case.
    /// The allowOnly32Bit boolean parameter will be set to True.
    /// Verifies that the result is the expected one, in accordance with the expected 64-bit exclusivity.
    /// </summary>
    /// <param name="testCase">Target test case.</param>
    [TestCaseSource(nameof(_testCases))]
    public void IsValidOn32BitTest(ExpressionTestCase testCase)
    {
        bool result = PointerPath.IsValid(testCase.Expression, true);
        Assert.That(result, Is.EqualTo(testCase is { ShouldBeValid: true, Expect64BitOnly: false }),
            testCase.Explanation);
    }
    
    /// <summary>
    /// Tests <see cref="PointerPath.TryParse(string,bool)"/> for the given expression test case.
    /// The allowOnly32Bit boolean parameter will be set to False.
    /// Verifies that the result is null when the expression is expected to be invalid, or otherwise that each property
    /// of the resulting address matches expectations.
    /// </summary>
    /// <param name="testCase">Target test case.</param>
    [TestCaseSource(nameof(_testCases))]
    public void TryParseOn64BitTest(ExpressionTestCase testCase)
    {
        var result = PointerPath.TryParse(testCase.Expression);
        AssertResultingAddress(result, testCase);
    }
    
    /// <summary>
    /// Tests <see cref="PointerPath.TryParse(string,bool)"/> for the given expression test case.
    /// The allowOnly32Bit boolean parameter will be set to True.
    /// Verifies that the result is null when the expression is expected to be invalid in accordance with the expected
    /// 64-bit exclusivity, or otherwise that each property of the resulting address matches expectations.
    /// </summary>
    /// <param name="testCase">Target test case.</param>
    [TestCaseSource(nameof(_testCases))]
    public void TryParseOn32BitTest(ExpressionTestCase testCase)
    {
        var result = PointerPath.TryParse(testCase.Expression, true);
        if (testCase is { ShouldBeValid: true, Expect64BitOnly: true })
            Assert.That(result, Is.Null, testCase.Explanation);
        else
            AssertResultingAddress(result, testCase);
    }
    
    /// <summary>
    /// Tests <see cref="PointerPath(string)"/> for the given expression test case.
    /// Verifies that an exception is thrown when the expression is expected to be invalid, or otherwise that each
    /// property of the resulting address matches expectations.
    /// </summary>
    /// <param name="testCase">Target test case.</param>
    [TestCaseSource(nameof(_testCases))]
    public void ConstructorTest(ExpressionTestCase testCase)
    {
        try
        {
            var result = new PointerPath(testCase.Expression);
            AssertResultingAddress(result, testCase);
        }
        catch (ArgumentException)
        {
            Assert.That(testCase.ShouldBeValid, Is.False, testCase.Explanation);
        }
    }

    /// <summary>
    /// Tests the implicit conversion operator from string to <see cref="PointerPath"/>.
    /// The resulting pointer path should be identical to a pointer path built using the constructor. 
    /// </summary>
    /// <param name="expression">Target expression to be converted.</param>
    [TestCase("1F016644,13,A0,0")]
    public void ImplicitConvertFromStringTest(string expression)
    {
        PointerPath convertedPath = expression;
        var constructedPath = new PointerPath(expression);
        Assert.That(convertedPath.Expression, Is.EqualTo(constructedPath.Expression),
            "The expression of a pointer path produced by implicit conversion should be identical to that of a pointer path produced by the constructor.");
    }

    /// <summary>
    /// Asserts that the given result matches the expectations of the specified test case.
    /// </summary>
    /// <param name="result">Pointer path to check against the test case expectations.</param>
    /// <param name="testCase">Target test case.</param>
    private void AssertResultingAddress(PointerPath? result, ExpressionTestCase testCase)
    {
        if (!testCase.ShouldBeValid)
        {
            Assert.That(result, Is.Null, testCase.Explanation);
            return;
        }
        
        Assert.That(result, Is.Not.Null, testCase.Explanation);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Expression, Is.EqualTo(testCase.Expression), testCase.Explanation);
            Assert.That(result.BaseModuleName, Is.EqualTo(testCase.ExpectedModuleName), testCase.Explanation);
            Assert.That(result.BaseModuleOffset, Is.EqualTo(testCase.ExpectedModuleOffset), testCase.Explanation);
            Assert.That(result.PointerOffsets, Is.EquivalentTo(testCase.ExpectedPointerOffsets!), testCase.Explanation);
            Assert.That(result.IsStrictly64Bit, Is.EqualTo(testCase.Expect64BitOnly), testCase.Explanation);
        });
    }

    /// <summary>
    /// Builds a pointer path from direct pointers (as opposed to an expression), and gets the expression from it.
    /// Checks that the expression matches expectations.
    /// </summary>
    [Test]
    public void GetExpressionFromDirectPointerPathTest()
    {
        var pointerPath = new PointerPath(new UIntPtr(0x1F016644), 0x4, -0x1C);
        Assert.That(pointerPath.Expression, Is.EqualTo("1F016644,4,-1C"));
    }
    
    /// <summary>
    /// Builds a pointer path from a module name, a module offset, and a sequence of pointer offsets, and gets the
    /// expression from it.
    /// Checks that the expression matches expectations.
    /// </summary>
    [Test]
    public void GetExpressionFromDirectPointerPathWithModuleNameTest()
    {
        var pointerPath = new PointerPath("mygame.exe", 0xC16, 0x4, -0x1C);
        Assert.That(pointerPath.Expression, Is.EqualTo("\"mygame.exe\"+C16,4,-1C"));
    }
}