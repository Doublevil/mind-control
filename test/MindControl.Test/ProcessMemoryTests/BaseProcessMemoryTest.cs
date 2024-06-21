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
    /// <remarks>The type prefix is dynamic in reality. Here, we use a stub array with only 0, which is enough to serve
    /// our purposes for the tests.</remarks>
    protected static readonly StringSettings DotNetStringSettings = new(Encoding.Unicode, true,
        new StringLengthPrefix(4, StringLengthUnit.Characters), new byte[8]);
    
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
    protected const int IndexOfOutputOuterBool = 0;
    /// <summary>Index, in the target app output, of the outer class instance byte value.</summary>
    protected const int IndexOfOutputOuterByte = 1;
    /// <summary>Index, in the target app output, of the outer class instance int value.</summary>
    protected const int IndexOfOutputOuterInt = 2;
    /// <summary>Index, in the target app output, of the outer class instance uint value.</summary>
    protected const int IndexOfOutputOuterUint = 3;
    /// <summary>Index, in the target app output, of the outer class instance string value.</summary>
    protected const int IndexOfOutputOuterString = 4;
    /// <summary>Index, in the target app output, of the outer class instance long value.</summary>
    protected const int IndexOfOutputOuterLong = 5;
    /// <summary>Index, in the target app output, of the outer class instance ulong value.</summary>
    protected const int IndexOfOutputOuterUlong = 6;
    /// <summary>Index, in the target app output, of the inner class instance byte value.</summary>
    protected const int IndexOfOutputInnerByte = 7;
    /// <summary>Index, in the target app output, of the inner class instance int value.</summary>
    protected const int IndexOfOutputInnerInt = 8;
    /// <summary>Index, in the target app output, of the inner class instance long value.</summary>
    protected const int IndexOfOutputInnerLong = 9;
    /// <summary>Index, in the target app output, of the outer class instance short value.</summary>
    protected const int IndexOfOutputOuterShort = 10;
    /// <summary>Index, in the target app output, of the outer class instance ushort value.</summary>
    protected const int IndexOfOutputOuterUshort = 11;
    /// <summary>Index, in the target app output, of the outer class instance float value.</summary>
    protected const int IndexOfOutputOuterFloat = 12;
    /// <summary>Index, in the target app output, of the outer class instance double value.</summary>
    protected const int IndexOfOutputOuterDouble = 13;
    /// <summary>Index, in the target app output, of the outer class instance byte array value.</summary>
    protected const int IndexOfOutputOuterByteArray = 14;
    
    /// <summary>
    /// Gets the pointer path for the value at the specified index by order of output of the target app.
    /// </summary>
    /// <param name="index">Index of the value to get the pointer path for, by order of output of the target app.
    /// </param>
    /// <returns>Pointer path to the value at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the index is not recognized.</exception>
    protected PointerPath GetPointerPathForValueAtIndex(int index) => OuterClassPointer.ToString("X") + index switch
    {
        IndexOfOutputOuterBool => Is64Bit ? "+48" : "+38",
        IndexOfOutputOuterByte => Is64Bit ? "+49" : "+39",
        IndexOfOutputOuterInt => Is64Bit ? "+38" : "+28",
        IndexOfOutputOuterUint => Is64Bit ? "+3C" : "+2C",
        IndexOfOutputOuterString => Is64Bit ? "+8" : "+1C",
        IndexOfOutputOuterLong => Is64Bit ? "+20" : "+4",
        IndexOfOutputOuterUlong => Is64Bit ? "+28" : "+C",
        IndexOfOutputInnerLong => Is64Bit ? "+10,8" : "+20,4",
        IndexOfOutputOuterShort => Is64Bit ? "+44" : "+34",
        IndexOfOutputOuterUshort => Is64Bit ? "+46" : "+36",
        IndexOfOutputOuterFloat => Is64Bit ? "+40" : "+30",
        IndexOfOutputOuterDouble => Is64Bit ? "+30" : "+14",
        IndexOfOutputOuterByteArray => Is64Bit ? "+18,10" : "+24,8",
        _ => throw new ArgumentOutOfRangeException(nameof(index))
    };
    
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