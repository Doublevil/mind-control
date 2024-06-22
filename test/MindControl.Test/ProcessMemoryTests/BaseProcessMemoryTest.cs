using System.Diagnostics;
using System.Globalization;
using System.Text;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Base class for tests that use a <see cref="MindControl.ProcessMemory"/>.
/// Executes a TargetApp process and provides a <see cref="ProcessMemory"/> instance, along with methods to manipulate
/// the process and general test helpers around the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class BaseProcessMemoryTest
{
    /// <summary>Name of the main module of the target app.</summary>
    protected const string MainModuleName = "MindControl.Test.TargetApp.dll";
    
    /// <summary>Settings that apply to strings used in our target .net process.</summary>
    /// <remarks>The type prefix is dynamic in reality. Here, we use a stub array with the right size that only holds
    /// zeroes, which is enough to serve our purposes for the tests.</remarks>
    protected StringSettings GetDotNetStringSettings()
        => new(Encoding.Unicode, true, new StringLengthPrefix(4, StringLengthUnit.Characters),
            new byte[Is64Bit ? 8 : 4]);
    
    private Process? _targetProcess;
    protected ProcessMemory? TestProcessMemory;
    protected UIntPtr OuterClassPointer;

    /// <summary>Gets a boolean value defining which version of the target app is used.</summary>
    protected virtual bool Is64Bit => true;
    
    /// <summary>
    /// Initializes the necessary instances for the tests.
    /// </summary>
    [SetUp]
    public void Initialize()
    {
        _targetProcess = StartTargetAppProcess(Is64Bit);
        string? line = _targetProcess.StandardOutput.ReadLine();
        if (!UIntPtr.TryParse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out OuterClassPointer))
            throw new Exception($"Could not read the outer class pointer output by the app: \"{line}\".");
        
        TestProcessMemory = ProcessMemory.OpenProcess(_targetProcess);
    }

    /// <summary>
    /// Starts the target app and returns its process.
    /// </summary>
    /// <param name="run64BitVersion">Whether to run the 64-bit version of the target app. Default is true.</param>
    public static Process StartTargetAppProcess(bool run64BitVersion = true)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = $"./TargetApp/{(run64BitVersion ? "x64" : "x86")}/MindControl.Test.TargetApp.exe",
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                StandardOutputEncoding = Encoding.UTF8
            }
        };
        process.Start();
        return process;
    }

    /// <summary>
    /// Disposes everything and kills the target app process at the end of the test.
    /// </summary>
    [TearDown]
    public void CleanUp()
    {
        TestProcessMemory?.Dispose();
        _targetProcess?.Kill();
        _targetProcess?.Dispose();
        // Make sure the process is exited before going on, otherwise it could cause other tests to fail. 
        Thread.Sleep(250);
    }

    private int _currentStep = 0;
    protected readonly string[] FinalResults = new string[15]; 
    
    /// <summary>
    /// Sends input to the target app process in order to make it continue to the next step.
    /// </summary>
    protected string? ProceedToNextStep()
    {
        if (_currentStep >= 2)
            throw new InvalidOperationException("The target app has already reached its final step.");
        
        _targetProcess?.StandardInput.WriteLine();
        _targetProcess?.StandardInput.Flush();

        if (++_currentStep == 2)
        {
            for (int i = 0; i < FinalResults.Length; i++)
            {
                FinalResults[i] = _targetProcess!.StandardOutput.ReadLine()!;
            }
        }
        else
        {
            return _targetProcess!.StandardOutput.ReadLine();
        }

        return null;
    }

    /// <summary>
    /// Sends input to the target app process in order to make it continue to the end.
    /// </summary>
    protected void ProceedUntilProcessEnds()
    {
        ProceedToNextStep();
        ProceedToNextStep();
    }

    /// <summary>Initial value of the outer class instance bool value.</summary>
    protected const bool InitialBoolValue = true;
    /// <summary>Initial value of the outer class instance byte value.</summary>
    protected const byte InitialByteValue = 0xAC;
    /// <summary>Initial value of the outer class instance int value.</summary>
    protected const int InitialIntValue = -7651;
    /// <summary>Initial value of the outer class instance uint value.</summary>
    protected const uint InitialUIntValue = 6781631;
    /// <summary>Initial value of the outer class instance string value.</summary>
    protected const string InitialStringValue = "ThisIsÄString";
    /// <summary>Initial value of the outer class instance long value.</summary>
    protected const long InitialLongValue = -65746876815103L;
    /// <summary>Initial value of the outer class instance ulong value.</summary>
    protected const ulong InitialULongValue = 76354111324644L;
    /// <summary>Initial value of the inner class instance byte value.</summary>
    protected const byte InitialInnerByteValue = 0xAA;
    /// <summary>Initial value of the inner class instance int value.</summary>
    protected const int InitialInnerIntValue = 1111111;
    /// <summary>Initial value of the inner class instance long value.</summary>
    protected const long InitialInnerLongValue = 999999999999L;
    /// <summary>Initial value of the outer class instance short value.</summary>
    protected const short InitialShortValue = -7777;
    /// <summary>Initial value of the outer class instance ushort value.</summary>
    protected const ushort InitialUShortValue = 8888;
    /// <summary>Initial value of the outer class instance float value.</summary>
    protected const float InitialFloatValue = 3456765.323f;
    /// <summary>Initial value of the outer class instance double value.</summary>
    protected const double InitialDoubleValue = 79879131651.333454;
    /// <summary>Initial value of the outer class instance byte array value.</summary>
    protected static readonly byte[] InitialByteArrayValue = [0x11, 0x22, 0x33, 0x44];
    
    /// <summary>Expected final value of the outer class instance bool value.</summary>
    protected const bool ExpectedFinalBoolValue = false;
    /// <summary>Expected final value of the outer class instance byte value.</summary>
    protected const byte ExpectedFinalByteValue = 0xDC;
    /// <summary>Expected final value of the outer class instance int value.</summary>
    protected const int ExpectedFinalIntValue = 987411;
    /// <summary>Expected final value of the outer class instance uint value.</summary>
    protected const uint ExpectedFinalUIntValue = 444763;
    /// <summary>Expected final value of the outer class instance string value.</summary>
    protected const string ExpectedFinalStringValue = "ThisIsALongerStrîngWith文字化けチェック";
    /// <summary>Expected final value of the outer class instance long value.</summary>
    protected const long ExpectedFinalLongValue = -777654646516513;
    /// <summary>Expected final value of the outer class instance ulong value.</summary>
    protected const ulong ExpectedFinalULongValue = 34411111111164;
    /// <summary>Expected final value of the inner class instance byte value.</summary>
    protected const byte ExpectedFinalInnerByteValue = 0xAD;
    /// <summary>Expected final value of the inner class instance int value.</summary>
    protected const int ExpectedFinalInnerIntValue = 64646321;
    /// <summary>Expected final value of the inner class instance long value.</summary>
    protected const long ExpectedFinalInnerLongValue = 7777777777777;
    /// <summary>Expected final value of the outer class instance short value.</summary>
    protected const short ExpectedFinalShortValue = -8888;
    /// <summary>Expected final value of the outer class instance ushort value.</summary>
    protected const ushort ExpectedFinalUShortValue = 9999;
    /// <summary>Expected final value of the outer class instance float value.</summary>
    protected const float ExpectedFinalFloatValue = -123444.15f;
    /// <summary>Expected final value of the outer class instance double value.</summary>
    protected const double ExpectedFinalDoubleValue = -99879416311.4478;
    /// <summary>Expected final value of the outer class instance byte array value.</summary>
    protected static readonly byte[] ExpectedFinalByteArrayValue = [0x55, 0x66, 0x77, 0x88];
    
    /// <summary>
    /// Stores the line-by-line expected output of the target app.
    /// </summary>
    protected static readonly string[] ExpectedFinalValues =
    [
        "False",
        "220",
        "987411",
        "444763",
        "ThisIsALongerStrîngWith文字化けチェック",
        "-777654646516513",
        "34411111111164",
        "173",
        "64646321",
        "7777777777777",
        "-8888",
        "9999",
        "-123444.15",
        "-99879416311.4478",
        "85,102,119,136"
    ];

    /// <summary>Index, in the target app output, of the outer class instance bool value.</summary>
    protected const int IndexOfOutputBool = 0;
    /// <summary>Index, in the target app output, of the outer class instance byte value.</summary>
    protected const int IndexOfOutputByte = 1;
    /// <summary>Index, in the target app output, of the outer class instance int value.</summary>
    protected const int IndexOfOutputInt = 2;
    /// <summary>Index, in the target app output, of the outer class instance uint value.</summary>
    protected const int IndexOfOutputUInt = 3;
    /// <summary>Index, in the target app output, of the outer class instance string value.</summary>
    protected const int IndexOfOutputString = 4;
    /// <summary>Index, in the target app output, of the outer class instance long value.</summary>
    protected const int IndexOfOutputLong = 5;
    /// <summary>Index, in the target app output, of the outer class instance ulong value.</summary>
    protected const int IndexOfOutputULong = 6;
    /// <summary>Index, in the target app output, of the inner class instance byte value.</summary>
    protected const int IndexOfOutputInnerByte = 7;
    /// <summary>Index, in the target app output, of the inner class instance int value.</summary>
    protected const int IndexOfOutputInnerInt = 8;
    /// <summary>Index, in the target app output, of the inner class instance long value.</summary>
    protected const int IndexOfOutputInnerLong = 9;
    /// <summary>Index, in the target app output, of the outer class instance short value.</summary>
    protected const int IndexOfOutputShort = 10;
    /// <summary>Index, in the target app output, of the outer class instance ushort value.</summary>
    protected const int IndexOfOutputUShort = 11;
    /// <summary>Index, in the target app output, of the outer class instance float value.</summary>
    protected const int IndexOfOutputFloat = 12;
    /// <summary>Index, in the target app output, of the outer class instance double value.</summary>
    protected const int IndexOfOutputDouble = 13;
    /// <summary>Index, in the target app output, of the outer class instance byte array value.</summary>
    protected const int IndexOfOutputByteArray = 14;
    
    /// <summary>
    /// Gets the pointer path for the value at the specified index by order of output of the target app, regardless of
    /// its bitness.
    /// </summary>
    /// <param name="index">Index of the value to get the pointer path for, by order of output of the target app.
    /// </param>
    /// <returns>Pointer path to the value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is not recognized or invalid.</exception>
    protected PointerPath GetPointerPathForValueAtIndex(int index) => OuterClassPointer.ToString("X") + index switch
    {
        IndexOfOutputBool => Is64Bit ? "+48" : "+38",
        IndexOfOutputByte => Is64Bit ? "+49" : "+39",
        IndexOfOutputInt => Is64Bit ? "+38" : "+28",
        IndexOfOutputUInt => Is64Bit ? "+3C" : "+2C",
        IndexOfOutputString => Is64Bit ? "+8" : "+1C",
        IndexOfOutputLong => Is64Bit ? "+20" : "+4",
        IndexOfOutputULong => Is64Bit ? "+28" : "+C",
        IndexOfOutputInnerLong => Is64Bit ? "+10,8" : "+20,4",
        IndexOfOutputShort => Is64Bit ? "+44" : "+34",
        IndexOfOutputUShort => Is64Bit ? "+46" : "+36",
        IndexOfOutputFloat => Is64Bit ? "+40" : "+30",
        IndexOfOutputDouble => Is64Bit ? "+30" : "+14",
        IndexOfOutputByteArray => Is64Bit ? "+18,10" : "+24,8",
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
    
    /// <summary>
    /// Gets the address to the value at the specified index by order of output of the target app, regardless of its
    /// bitness.
    /// </summary>
    /// <param name="index">Index of the target value, by order of output of the target app.</param>
    /// <returns>Address of the value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is not recognized or invalid.</exception>
    protected UIntPtr GetAddressForValueAtIndex(int index) => OuterClassPointer + (UIntPtr)(index switch
    {
        IndexOfOutputBool => Is64Bit ? 0x48 : 0x38,
        IndexOfOutputByte => Is64Bit ? 0x49 : 0x39,
        IndexOfOutputInt => Is64Bit ? 0x38 : 0x28,
        IndexOfOutputUInt => Is64Bit ? 0x3C : 0x2C,
        IndexOfOutputString => Is64Bit ? 0x8 : 0x1C,
        IndexOfOutputLong => Is64Bit ? 0x20 : 0x4,
        IndexOfOutputULong => Is64Bit ? 0x28 : 0xC,
        IndexOfOutputShort => Is64Bit ? 0x44 : 0x34,
        IndexOfOutputUShort => Is64Bit ? 0x46 : 0x36,
        IndexOfOutputFloat => Is64Bit ? 0x40 : 0x30,
        IndexOfOutputDouble => Is64Bit ? 0x30 : 0x14,
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    });

    /// <summary>
    /// Gets the pointer path to the raw bytes of the output string of the target app, regardless of its bitness.
    /// </summary>
    protected PointerPath GetPathToRawStringBytes()
        => OuterClassPointer.ToString("X") + (Is64Bit ? "+8,C" : "+1C,8");
    
    /// <summary>
    /// Gets a pointer path that evaluates to an address pointing to a 0xFFFFFFFFFFFFFFFF (x64) or 0xFFFFFFFF (x86)
    /// value. Warning: the path itself does not evaluate to the max address, but to a pointer to it. To clarify with
    /// an example, this path will evaluate to say 0x4A64F850, and if you read a ulong at that address, you will get
    /// 0xFFFFFFFFFFFFFFFF.
    /// </summary>
    protected PointerPath GetPathToPointerToMaxAddress()
        => OuterClassPointer.ToString("X") + (Is64Bit ? "+10,10" : "+20,10");
    
    /// <summary>
    /// Gets the address of the pointer of the output string of the target app, regardless of its bitness.
    /// </summary>
    protected UIntPtr GetStringPointerAddress()
        => OuterClassPointer + (Is64Bit ? (UIntPtr)0x8 : 0x1C);

    /// <summary>
    /// Gets the maximum value that a pointer can have in the target app (which depends of its bitness).
    /// </summary>
    protected UIntPtr GetMaxPointerValue()
        => Is64Bit ? UIntPtr.MaxValue : uint.MaxValue;
    
    /// <summary>
    /// Asserts that among the final results output by the target app, the one at the given index matches the
    /// expected value, and all the other results are the known, untouched values. 
    /// </summary>
    /// <param name="index">Index of the final result to check against the <paramref name="expectedValue"/>.</param>
    /// <param name="expectedValue">Expected value of the final result at the specified index.</param>
    protected void AssertFinalResults(int index, string expectedValue)
    {
        for (int i = 0; i < ExpectedFinalValues.Length; i++)
        {
            string expectedValueAtIndex = i == index ? expectedValue : ExpectedFinalValues[i];
            Assert.That(FinalResults.ElementAtOrDefault(i), Is.EqualTo(expectedValueAtIndex));
        }
    }
    
    /// <summary>
    /// Asserts that the final results output by the target app match the expected values.
    /// </summary>
    protected void AssertExpectedFinalResults()
    {
        for (int i = 0; i < ExpectedFinalValues.Length; i++)
            Assert.That(FinalResults.ElementAtOrDefault(i), Is.EqualTo(ExpectedFinalValues[i]));
    }
}