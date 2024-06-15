using System.Diagnostics;
using System.Globalization;
using System.Text;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests <see cref="MindControl.ProcessMemory"/>'s general features.
/// For memory reading tests, see <see cref="ProcessMemoryReadTest"/>.
/// For memory path evaluation tests, see <see cref="ProcessMemoryEvaluateTest"/>.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryTest
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
    
    /// <summary>
    /// Initializes the necessary instances for the tests.
    /// </summary>
    [SetUp]
    public void Initialize()
    {
        _targetProcess = StartTargetAppProcess();
        string? line = _targetProcess.StandardOutput.ReadLine();
        if (!UIntPtr.TryParse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out OuterClassPointer))
            throw new Exception($"Could not read the outer class pointer output by the app: \"{line}\".");
        
        TestProcessMemory = ProcessMemory.OpenProcess(_targetProcess);
    }

    /// <summary>
    /// Starts the target app and returns its process.
    /// </summary>
    public static Process StartTargetAppProcess()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "./MindControl.Test.TargetApp.exe",
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