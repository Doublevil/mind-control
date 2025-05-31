using MindControl.Modules;
using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the features of the <see cref="RemoteModule"/> class.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class RemoteModuleTest : BaseProcessMemoryTest
{
    /// <summary>
    /// Tests the <see cref="ProcessMemory.GetModule"/> method with an invalid module name.
    /// </summary>
    [Test]
    public void GetModuleWithInvalidNameTest()
    {
        var module = TestProcessMemory!.GetModule("this module does not exist.dll");
        Assert.That(module, Is.Null);
    }
    
    /// <summary>
    /// Tests the <see cref="RemoteModule.GetRange"/> method on an existing module.
    /// </summary>
    [Test]
    public void GetRangeTest()
    {
        var module = TestProcessMemory!.GetModule("kernel32.dll") ?? throw new Exception("Module not found");
        var range = module.GetRange();
        Assert.That(range.Start, Is.EqualTo((UIntPtr)module.GetManagedModule().BaseAddress));
        Assert.That(range.GetSize(), Is.EqualTo((ulong)module.GetManagedModule().ModuleMemorySize));
    }
    
    /// <summary>
    /// Tests the <see cref="RemoteModule.ReadExportTable"/> method on kernel32.dll.
    /// Expect more than 1000 functions (kernel32.dll is packed up!) and that in particular LoadLibraryW is among them.
    /// Also check that the address of functions are within the bounds of the module.
    /// </summary>
    [Test]
    public void ReadExportTableWithKernel32Test()
    {
        var module = TestProcessMemory!.GetModule("kernel32.dll") ?? throw new Exception("Module not found");
        var exportTable = module.ReadExportTable();
        Assert.That(exportTable.IsSuccess, Is.True, () => exportTable.Failure.ToString());
        Assert.That(exportTable.Value, Has.Count.GreaterThan(1000));
        Assert.That(exportTable.Value.ContainsKey("LoadLibraryW"));
        Assert.That(exportTable.Value.Values.Select(t => module.GetRange().Contains(t)), Is.All.True);
    }
}

/// <summary>
/// Runs the tests from <see cref="RemoteModuleTest"/> with a 32-bit version of the target app.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class RemoteModuleTestX86 : RemoteModuleTest
{
    /// <summary>Gets a boolean value defining which version of the target app is used.</summary>
    protected override bool Is64Bit => false;
}