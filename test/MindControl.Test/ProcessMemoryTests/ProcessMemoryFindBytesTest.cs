using NUnit.Framework;

namespace MindControl.Test.ProcessMemoryTests;

/// <summary>
/// Tests the features of the <see cref="ProcessMemory"/> class related to finding bytes in the target process.
/// </summary>
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ProcessMemoryFindBytesTest : BaseProcessMemoryTest
{
    /// <summary>
    /// Tests the <see cref="ProcessMemory.FindBytes"/> method with a known fixed bytes pattern.
    /// The search is performed in the main module of the target process, with default search options.
    /// We expect to find 1 occurrence of the pattern in the main module, as observed manually with hacking tools.
    /// </summary>
    [Test]
    public void FindBytesWithKnownFixedBytesPatternTest()
    {
        var range = TestProcessMemory!.GetModule(MainModuleName)!.GetRange();
        var results = TestProcessMemory!.FindBytes("4D 79 53 74 72 69 6E 67 56 61 6C 75 65", range).ToArray();
        
        // We won't verify the exact address, because it can change between runs and with modifications in the target
        // process. So we will perform property-based tests instead.
        
        // We know there should be 1 occurrence of the pattern in the main module from observations with hacking tools
        Assert.That(results, Has.Length.EqualTo(1));
        
        // Verify that the result is within the range of the main module
        Assert.That(range.Contains(results.Single()), Is.True);
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.FindBytes"/> method with a known bytes pattern with wildcards.
    /// The search is performed in the main module of the target process, with default search options.
    /// We expect to find 3 occurrences of the pattern in the main module, as observed manually with hacking tools.
    /// </summary>
    [Test]
    public void FindBytesWithKnownMaskedBytesPatternTest()
    {
        var range = TestProcessMemory!.GetModule(MainModuleName)!.GetRange();
        var results = TestProcessMemory!.FindBytes("4D 79 ?? ?? ?? ?? ?? ?? 56 61 6C 75 65", range).ToArray();
        
        // We know there should be 3 occurrences of the pattern in the main module from observations with hacking tools
        Assert.That(results, Has.Length.EqualTo(3));
        
        // We won't verify the exact addresses, because they can change between runs and with modifications in the
        // target process. So we will perform property-based tests instead.
        
        // Verify that there are no duplicates
        Assert.That(results.Distinct().Count(), Is.EqualTo(3));
        
        // Verify that the results are within the range of the main module
        foreach (var result in results)
            Assert.That(range.Contains(result), Is.True);
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.FindBytes"/> method with a known bytes pattern with partial wildcards.
    /// The search is performed in the main module of the target process, with default search options.
    /// We expect to find 1 occurrence of the pattern in the main module, as observed manually with hacking tools.
    /// </summary>
    [Test]
    public void FindBytesWithKnownPartialMasksBytesPatternTest()
    {
        var range = TestProcessMemory!.GetModule(MainModuleName)!.GetRange();
        var results = TestProcessMemory!.FindBytes("4D 79 53 74 72 69 6E 6? ?6 61 6C 75 65", range).ToArray();
        
        // We won't verify the exact address, because it can change between runs and with modifications in the target
        // process. So we will perform property-based tests instead.
        
        // We know there should be 1 occurrence of the pattern in the main module from observations with hacking tools
        Assert.That(results, Has.Length.EqualTo(1));
        
        // Verify that the result is within the range of the main module
        Assert.That(range.Contains(results.Single()), Is.True);
    }
    
    /// <summary>
    /// Tests the <see cref="ProcessMemory.FindBytesAsync"/> method with a known fixed bytes pattern.
    /// The search is performed in the main module of the target process, with default search options.
    /// We expect to find 1 occurrence of the pattern in the main module, as observed manually with hacking tools.
    /// </summary>
    [Test]
    public async Task FindBytesAsyncWithKnownFixedBytesPatternTest()
    {
        var range = TestProcessMemory!.GetModule(MainModuleName)!.GetRange();
        var results = await TestProcessMemory!.FindBytesAsync("4D 79 53 74 72 69 6E 67 56 61 6C 75 65", range)
            .ToArrayAsync();
        
        // We won't verify the exact address, because it can change between runs and with modifications in the target
        // process. So we will perform property-based tests instead.
        
        // We know there should be 1 occurrence of the pattern in the main module from observations with hacking tools
        Assert.That(results, Has.Length.EqualTo(1));
        
        // Verify that the result is within the range of the main module
        Assert.That(range.Contains(results.Single()), Is.True);
    }
}