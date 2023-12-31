﻿namespace MindControl.Native;

/// <summary>
/// Protection attributes attributed to a memory page.
/// </summary>
[Flags]
public enum MemoryProtection : uint
{
    /// <summary>
    /// Enables execute access to the committed region of pages. An attempt to write to the committed region results in
    /// an access violation.
    /// </summary>
    Execute = 0x10,
    
    /// <summary>
    /// Enables execute or read-only access to the committed region of pages. An attempt to write to the committed
    /// region results in an access violation.
    /// </summary>
    ExecuteRead = 0x20,
    
    /// <summary>
    /// Enables execute, read-only, or read/write access to the committed region of pages.
    /// </summary>
    ExecuteReadWrite = 0x40,
    
    /// <summary>
    /// Enables execute, read-only, or copy-on-write access to a mapped view of a file mapping object. An attempt to
    /// write to a committed copy-on-write page results in a private copy of the page being made for the process.
    /// The private page is marked as <see cref="ExecuteReadWrite"/>, and the change is written to the new page.
    /// </summary>
    ExecuteWriteCopy = 0x80,
    
    /// <summary>
    /// Disables all access to the committed region of pages. An attempt to read from, write to, or execute the
    /// committed region results in an access violation.
    /// </summary>
    NoAccess = 0x01,
    
    /// <summary>
    /// Enables read-only access to the committed region of pages. An attempt to write to the committed region results
    /// in an access violation. If Data Execution Prevention is enabled, an attempt to execute code in the committed
    /// region results in an access violation.
    /// </summary>
    ReadOnly = 0x02,
    
    /// <summary>
    /// Enables read-only or read/write access to the committed region of pages. If Data Execution Prevention is
    /// enabled, attempting to execute code in the committed region results in an access violation.
    /// </summary>
    ReadWrite = 0x04,
    
    /// <summary>
    /// Enables read-only or copy-on-write access to a mapped view of a file mapping object. An attempt to write to a
    /// committed copy-on-write page results in a private copy of the page being made for the process. The private page
    /// is marked as <see cref="ReadWrite"/>, and the change is written to the new page. If Data Execution Prevention is
    /// enabled, attempting to execute code in the committed region results in an access violation.
    /// </summary>
    WriteCopy = 0x08,
    
    // The following are modifiers that can be used in combination with the previous options to set a page protection.
    
    /// <summary>
    /// Pages in the region become guard pages. Any attempt to access a guard page causes the system to raise a
    /// STATUS_GUARD_PAGE_VIOLATION exception and turn off the guard page status. Guard pages thus act as a one-time
    /// access alarm. When an access attempt leads the system to turn off guard page status, the underlying page
    /// protection takes over. If a guard page exception occurs during a system service, the service typically returns
    /// a failure status indicator. This value cannot be used with <see cref="NoAccess"/>.
    /// </summary>
    PageGuard = 0x100,
    
    /// <summary>
    /// Sets all pages to be non-cachable. Applications should not use this attribute except when explicitly required
    /// for a device. Using the interlocked functions with memory that is mapped with SEC_NOCACHE can result in an
    /// EXCEPTION_ILLEGAL_INSTRUCTION exception. This flag cannot be used with the <see cref="PageGuard"/>,
    /// <see cref="NoAccess"/>, or <see cref="WriteCombine"/> flags.
    /// </summary>
    NoCache = 0x200,
    
    /// <summary>
    /// Sets all pages to be write-combined. Applications should not use this attribute except when explicitly required
    /// for a device. Using the interlocked functions with memory that is mapped as write-combined can result in an
    /// EXCEPTION_ILLEGAL_INSTRUCTION exception. This flag cannot be specified with the <see cref="NoAccess"/>,
    /// <see cref="PageGuard"/>, and <see cref="NoCache"/> flags.
    /// </summary>
    WriteCombine = 0x400
}
