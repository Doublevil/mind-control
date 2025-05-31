using MindControl.Modules;
using MindControl.Results;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to injecting a library.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryInjectionTest : BaseProcessMemoryTest
{
    /// <summary>
    /// Gets a relative path to the injected library appropriate for the given bitness, or, by default, for
    /// the bitness of the test.
    /// </summary>
    /// <param name="use64Bit">Whether to use the 64-bit version of the library. Default is the bitness of the
    /// test.</param>
    protected string GetInjectedLibraryPath(bool? use64Bit = null)
    {
        use64Bit ??= Is64Bit;
        return $"./InjectedLibrary/{(use64Bit.Value ? "x64" : "x86")}/MindControl.Test.InjectedLibrary.dll";
    }

    /// <summary>
    /// Ensures the tests are correctly set up before running.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        if (!File.Exists(GetInjectedLibraryPath()))
        {
            throw new FileNotFoundException("Injected library not found. Make sure the project \"MindControl.Test.InjectedLibrary\" was built before running the tests.");
        }
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.InjectLibrary(string)"/> method.
    /// After injecting the library, the target process should output "Injected library attached", which is the text
    /// printed by code run from the injected library.
    /// </summary>
    [Test]
    public void InjectLibraryTest()
    {
        var result = TestProcessMemory!.InjectLibrary(GetInjectedLibraryPath());
        Assert.That(result.IsSuccess, Is.True, () => result.Failure.ToString());
        var output = ProceedToNextStep();
        Assert.That(output, Is.EqualTo("Injected library attached"));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.InjectLibrary(string)"/> method.
    /// Does the same as <see cref="InjectLibraryTest"/>, but with a DLL that has a path containing spaces and non-ASCII
    /// characters.
    /// </summary>
    [Test]
    public void InjectLibraryWithNonAsciiPathTest()
    {
        string injectedLibraryPath = GetInjectedLibraryPath();
        const string targetFileName = "憂 鬱.dll";
        string targetPath = Path.Combine(Path.GetDirectoryName(injectedLibraryPath)!, targetFileName);
        
        File.Copy(GetInjectedLibraryPath(), targetPath, true);
        var result = TestProcessMemory!.InjectLibrary(targetPath);
        Assert.That(result.IsSuccess, Is.True);
        var output = ProceedToNextStep();
        Assert.That(output, Is.EqualTo("Injected library attached"));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.InjectLibrary(string)"/> method.
    /// Specify a path to a non-existent library file.
    /// The method should fail with a <see cref="LibraryFileNotFoundFailure"/>.
    /// </summary>
    [Test]
    public void InjectLibraryWithLibraryFileNotFoundTest()
    {
        const string path = "./NonExistentLibrary.dll";
        var result = TestProcessMemory!.InjectLibrary(path);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.TypeOf<LibraryFileNotFoundFailure>());
        var failure = (LibraryFileNotFoundFailure)result.Failure;
        Assert.That(failure.LibraryPath, Has.Length.GreaterThan(path.Length)); // We expect a full path
        Assert.That(failure.LibraryPath, Does.EndWith("NonExistentLibrary.dll"));
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.InjectLibrary(string)"/> method with a detached process.
    /// The method should fail with a <see cref="DetachedProcessFailure"/>.
    /// </summary>
    [Test]
    public void InjectLibraryWithDetachedProcessTest()
    {
        TestProcessMemory!.Dispose();
        var result = TestProcessMemory.InjectLibrary(GetInjectedLibraryPath());
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<DetachedProcessFailure>());
    }
}

/// <summary>
/// Runs the tests from <see cref="ProcessMemoryInjectionTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryInjectionTestX86 : ProcessMemoryInjectionTest
{
    /// <summary>Gets a boolean value defining which version of the target app is used.</summary>
    protected override bool Is64Bit => false;
    
    /// <summary>
    /// Tests <see cref="ProcessMemory.InjectLibrary(string)"/> with a 64-bit library on a 32-bit process.
    /// Expect a <see cref="LibraryLoadFailure"/>.
    /// </summary>
    [Test]
    public void InjectX64LibraryOnX86ProcessTest()
    {
        var result = TestProcessMemory!.InjectLibrary(GetInjectedLibraryPath(true));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Failure, Is.InstanceOf<LibraryLoadFailure>());
    }
}