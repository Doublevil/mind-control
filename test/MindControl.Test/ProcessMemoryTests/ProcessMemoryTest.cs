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
    protected void ProceedToNextStep()
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
            _targetProcess!.StandardOutput.ReadLine();
        }
    }

    /// <summary>
    /// Sends input to the target app process in order to make it continue to the end.
    /// </summary>
    protected void ProceedUntilProcessEnds()
    {
        ProceedToNextStep();
        ProceedToNextStep();
    }
}