using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to injecting a library.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryInjectionTest : ProcessMemoryTest
{
    private const string InjectedLibraryPath = "./MindControl.Test.InjectedLibrary.dll";

    /// <summary>
    /// Ensures the tests are correctly set up before running.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        if (!File.Exists(InjectedLibraryPath))
        {
            throw new FileNotFoundException("Injected library not found. Make sure the project \"MindControl.Test.InjectedLibrary\" was built before running the tests.");
        }
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.InjectLibrary"/> method.
    /// After injecting the library, the target process should output "Injected library attached", which is the text
    /// printed by code run from the injected library.
    /// </summary>
    [Test]
    public void InjectLibraryTest()
    {
        TestProcessMemory!.InjectLibrary(InjectedLibraryPath);
        var output = ProceedToNextStep();
        Assert.That(output, Is.EqualTo("Injected library attached"));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.InjectLibrary"/> method.
    /// Does the same as <see cref="InjectLibraryTest"/>, but with a DLL that has a path containing spaces and non-ASCII
    /// characters.
    /// </summary>
    [Test]
    public void InjectLibraryWithNonAsciiPathTest()
    {
        const string targetPath = "憂 鬱.dll";
        File.Copy(InjectedLibraryPath, targetPath, true);
        TestProcessMemory!.InjectLibrary(targetPath);
        var output = ProceedToNextStep();
        Assert.That(output, Is.EqualTo("Injected library attached"));
    }
}