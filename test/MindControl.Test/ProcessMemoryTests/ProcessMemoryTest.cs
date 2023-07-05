using System.Diagnostics;
using System.Globalization;
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
    protected IntPtr OuterClassPointer;
    
    /// <summary>
    /// Initializes the necessary instances for the tests.
    /// </summary>
    [SetUp]
    public void Initialize()
    {
        _targetProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "../../../MindControl.Test.TargetApp/bin/Release/MindControl.Test.TargetApp.exe",
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            }
        };
        
        try
        {
            _targetProcess.Start();
        }
        catch (Exception e)
        {
            throw new Exception(
                "An error occurred while attempting to start the testing target app. Please check that the MindControl.Test.TargetApp project has been built in Release before running the tests.", e);
        }
        
        string? line = _targetProcess.StandardOutput.ReadLine();
        if (!IntPtr.TryParse(line, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out OuterClassPointer))
            throw new Exception($"Could not read the outer class pointer output by the app: \"{line}\".");
        
        TestProcessMemory = ProcessMemory.OpenProcess(_targetProcess);
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
    }

    private int _currentStep = 0;
    protected readonly string[] FinalResults = new string[8]; 
    
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
    /// This test only ensures that the setup works, i.e. that opening a process as a <see cref="MindControl.ProcessMemory"/>
    /// instance won't throw an exception.
    /// </summary>
    [Test]
    public void OpenProcessTest() { }
}