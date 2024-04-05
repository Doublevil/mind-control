namespace MindControl.Native;

/// <summary>
/// Represents the system information of the current computer.
/// </summary>
internal struct SystemInfo
{
    /// <summary>
    /// The processor architecture of the installed operating system.
    /// </summary>
    public ProcessorArchitecture ProcessorArchitecture;

    /// <summary>
    /// The page size and the granularity of page protection and commitment.
    /// </summary>
    public uint PageSize;

    /// <summary>
    /// A pointer to the lowest memory address accessible to applications and dynamic-link libraries (DLLs).
    /// </summary>
    public UIntPtr MinimumApplicationAddress;

    /// <summary>
    /// A pointer to the highest memory address accessible to applications and DLLs.
    /// </summary>
    public UIntPtr MaximumApplicationAddress;

    /// <summary>
    /// A mask representing the set of processors configured into the system.
    /// </summary>
    public IntPtr ActiveProcessorMask;

    /// <summary>
    /// The number of logical processors in the current group.
    /// </summary>
    public uint NumberOfProcessors;

    /// <summary>
    /// An obsolete member that is retained for compatibility. Use the ProcessorArchitecture member instead.
    /// </summary>
    public uint ProcessorType;

    /// <summary>
    /// The granularity for the starting address at which virtual memory can be allocated.
    /// </summary>
    public uint AllocationGranularity;

    /// <summary>
    /// The architecture-dependent processor level.
    /// </summary>
    public ushort ProcessorLevel;

    /// <summary>
    /// The architecture-dependent processor revision.
    /// </summary>
    public ushort ProcessorRevision;
}

/// <summary>
/// Represents the processor architecture of the installed operating system.
/// </summary>
internal enum ProcessorArchitecture : ushort
{
    /// <summary>
    /// Represents an x86 architecture.
    /// </summary>
    Intel = 0,

    /// <summary>
    /// Represents an ARM architecture.
    /// </summary>
    Arm = 5,

    /// <summary>
    /// Represents an Intel Itanium-based architecture.
    /// </summary>
    Ia64 = 6,

    /// <summary>
    /// Represents an x64 architecture (AMD or Intel).
    /// </summary>
    Amd64 = 9,

    /// <summary>
    /// Represents an ARM64 architecture.
    /// </summary>
    Arm64 = 12,

    /// <summary>
    /// Represents an unknown architecture.
    /// </summary>
    Unknown = 0xFFFF
}