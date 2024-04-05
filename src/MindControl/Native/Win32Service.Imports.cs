using System.Runtime.InteropServices;
#pragma warning disable CS0649

namespace MindControl.Native;

// This partial class contains the imports for the Win32 API functions used in the main class file.
public partial class Win32Service
{
    /// <summary>
    /// Retrieves information about the current system.
    /// </summary>
    /// <param name="lpSystemInfo">A pointer to a SYSTEM_INFO structure that receives the information.</param>
    [DllImport("kernel32.dll")]
    private static extern void GetSystemInfo(out SystemInfo lpSystemInfo);
    
    /// <summary>
    /// Opens an existing local process object.
    /// </summary>
    /// <param name="dwDesiredAccess">The access to the process object. This access right is checked against the
    /// security descriptor for the process. This parameter can be one or more of the process access rights.
    /// If the caller has enabled the SeDebugPrivilege privilege, the requested access is granted regardless of the
    /// contents of the security descriptor.</param>
    /// <param name="bInheritHandle">If this value is TRUE, processes created by this process will inherit the handle.
    /// Otherwise, the processes do not inherit this handle.</param>
    /// <param name="dwProcessId">The identifier of the local process to be opened.</param>
    /// <returns>
    /// If the function succeeds, the return value is an open handle to the specified process.
    /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(
        UInt32 dwDesiredAccess,
        bool bInheritHandle,
        Int32 dwProcessId
    );
    
    /// <summary>
    /// Determines whether the specified process is running under WOW64 or an Intel64 of x64 processor.
    /// </summary>
    /// <param name="hProcess">A handle to the process. The handle must have the PROCESS_QUERY_INFORMATION or
    /// PROCESS_QUERY_LIMITED_INFORMATION access right.</param>
    /// <param name="wow64Process">A pointer to a value that is set to TRUE if the process is running under WOW64 on an
    /// Intel64 or x64 processor. If the process is running under 32-bit Windows, the value is set to FALSE. If the
    /// process is a 32-bit application running under 64-bit Windows 10 on ARM, the value is set to FALSE. If the
    /// process is a 64-bit application running under 64-bit Windows, the value is also set to FALSE.</param>
    /// <returns>
    /// See <paramref name="wow64Process"/>If the function succeeds, the return value is a nonzero value.
    /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
    /// </returns>
    [DllImport("kernel32", SetLastError = true)]
    private static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);
    
    /// <summary>
    /// Retrieves information about a range of pages within the virtual address space of a specified process.
    /// </summary>
    /// <param name="hProcess">A handle to the process whose memory information is queried. The handle must have been
    /// opened with the PROCESS_QUERY_INFORMATION access right, which enables using the handle to read information from
    /// the process object.</param>
    /// <param name="lpAddress">A pointer to the base address of the region of pages to be queried. This value is
    /// rounded down to the next page boundary. To determine the size of a page on the host computer, use the
    /// GetSystemInfo function.</param>
    /// <param name="lpBuffer">A pointer to a MEMORY_BASIC_INFORMATION structure in which information about the
    /// specified page range is returned.</param>
    /// <param name="dwLength">The size of the buffer pointed to by the lpBuffer parameter, in bytes.</param>
    /// <returns>The actual number of bytes returned in the information buffer, or, if the operation fails, 0.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress,
        out MemoryBasicInformation32 lpBuffer, UIntPtr dwLength);

    /// <summary>
    /// Contains information about a range of pages in the virtual address space of a process.
    /// </summary>
    private struct MemoryBasicInformation32
    {
        /// <summary>
        /// A pointer to the base address of the region of pages.
        /// </summary>
        public uint BaseAddress;
        
        /// <summary>
        /// A pointer to the base address of a range of pages allocated by the VirtualAlloc function. The page pointed
        /// to by the BaseAddress member is contained within this allocation range.
        /// </summary>
        public uint AllocationBase;
        
        /// <summary>
        /// The memory protection option when the region was initially allocated. This member can be one of the memory
        /// protection constants or 0 if the caller does not have access.
        /// </summary>
        public uint AllocationProtect;
        
        /// <summary>
        /// The size of the region beginning at the base address in which all pages have identical attributes, in bytes.
        /// </summary>
        public uint RegionSize;
        
        /// <summary>
        /// The state of the pages in the region.
        /// </summary>
        public MemoryState State;
        
        /// <summary>
        /// The access protection of the pages in the region. This member is one of the values listed for the
        /// AllocationProtect member.
        /// </summary>
        public MemoryProtection Protect;
        
        /// <summary>
        /// The type of pages in the region.
        /// </summary>
        public PageType Type;
    }
    
    /// <summary>
    /// Retrieves information about a range of pages within the virtual address space of a specified process.
    /// </summary>
    /// <param name="hProcess">A handle to the process whose memory information is queried. The handle must have been
    /// opened with the PROCESS_QUERY_INFORMATION access right, which enables using the handle to read information from
    /// the process object.</param>
    /// <param name="lpAddress">A pointer to the base address of the region of pages to be queried. This value is
    /// rounded down to the next page boundary. To determine the size of a page on the host computer, use the
    /// GetSystemInfo function.</param>
    /// <param name="lpBuffer">A pointer to a MEMORY_BASIC_INFORMATION structure in which information about the
    /// specified page range is returned.</param>
    /// <param name="dwLength">The size of the buffer pointed to by the lpBuffer parameter, in bytes.</param>
    /// <returns>The actual number of bytes returned in the information buffer, or, if the operation fails, 0.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress,
        out MemoryBasicInformation64 lpBuffer, UIntPtr dwLength);
    
    /// <summary>
    /// Contains information about a range of pages in the virtual address space of a process.
    /// 64-bit variant, with the alignment values.
    /// </summary>
    private struct MemoryBasicInformation64
    {
        /// <summary>
        /// A pointer to the base address of the region of pages.
        /// </summary>
        public ulong BaseAddress;
        
        /// <summary>
        /// A pointer to the base address of a range of pages allocated by the VirtualAlloc function. The page pointed
        /// to by the BaseAddress member is contained within this allocation range.
        /// </summary>
        public ulong AllocationBase;
        
        /// <summary>
        /// The memory protection option when the region was initially allocated. This member can be one of the memory
        /// protection constants or 0 if the caller does not have access.
        /// </summary>
        public uint AllocationProtect;
        
        /// <summary>
        /// First alignment value, specific to the 64-bit variant of this structure.
        /// </summary>
        public uint __alignment1;
        
        /// <summary>
        /// The size of the region beginning at the base address in which all pages have identical attributes, in bytes.
        /// </summary>
        public ulong RegionSize;
        
        /// <summary>
        /// The state of the pages in the region.
        /// </summary>
        public MemoryState State;
        
        /// <summary>
        /// The access protection of the pages in the region. This member is one of the values listed for the
        /// AllocationProtect member.
        /// </summary>
        public MemoryProtection Protect;
        
        /// <summary>
        /// The type of pages in the region.
        /// </summary>
        public PageType Type;
        
        /// <summary>
        /// Second alignment value, specific to the 64-bit variant of this structure.
        /// </summary>
        public uint __alignment2;
    }
    
    /// <summary>
    /// Contains information about a range of pages in the virtual address space of a process.
    /// This is the bitness-agnostic version of <see cref="MemoryBasicInformation32"/> and
    /// <see cref="MemoryBasicInformation64"/>.
    /// </summary>
    /// <param name="BaseAddress">A pointer to the base address of the region of pages.</param>
    /// <param name="AllocationBase">A pointer to the base address of a range of pages allocated by the VirtualAlloc
    /// function. The page pointed to by the BaseAddress member is contained within this allocation range.</param>
    /// <param name="AllocationProtect">The memory protection option when the region was initially allocated. This
    /// member can be one of the memory protection constants or 0 if the caller does not have access.</param>
    /// <param name="RegionSize">The size of the region beginning at the base address in which all pages have identical
    /// attributes, in bytes.</param>
    /// <param name="State">The state of the pages in the region.</param>
    /// <param name="Protect">The access protection of the pages in the region. This member is one of the values listed
    /// for the AllocationProtect member.</param>
    /// <param name="Type">The type of pages in the region.</param>
    private record struct MemoryBasicInformation(UIntPtr BaseAddress, UIntPtr AllocationBase, uint AllocationProtect,
        UIntPtr RegionSize, MemoryState State, MemoryProtection Protect, PageType Type);
    
    /// <summary>
    /// Enumerates the memory allocation types, as returned by the VirtualQueryEx functions.
    /// </summary>
    private enum MemoryState : uint
    {
        /// <summary>
        /// Indicates committed pages for which physical storage has been allocated, either in memory or in the paging
        /// file on disk.
        /// </summary>
        Commit = 0x1000,
        
        /// <summary>
        /// Indicates free pages not accessible to the calling process and available to be allocated.
        /// </summary>
        Reserve = 0x2000,
        
        /// <summary>
        /// Indicates reserved pages where a range of the process's virtual address space is reserved without any
        /// physical storage being allocated.
        /// </summary>
        Free = 0x10000
    }

    /// <summary>
    /// Enumerates the page types, as returned by the VirtualQueryEx functions.
    /// </summary>
    private enum PageType : uint
    {
        /// <summary>
        /// Indicates that the memory pages within the region are mapped into the view of an image section.
        /// </summary>
        Image = 0x1000000,
        
        /// <summary>
        /// Indicates that the memory pages within the region are mapped into the view of a section.
        /// </summary>
        Mapped = 0x40000,
        
        /// <summary>
        /// Indicates that the memory pages within the region are private (that is, not shared by other processes).
        /// </summary>
        Private = 0x20000
    }
    
    /// <summary>
    /// Reserves, commits, or changes the state of a region of memory within the virtual address space of a specified
    /// process. The function initializes the memory it allocates to zero.
    /// </summary>
    /// <param name="hProcess">A handle to the process that the memory is allocated within.
    /// The handle must have the PROCESS_VM_OPERATION access right.</param>
    /// <param name="lpAddress">The pointer that specifies a desired starting address for the region of pages that you
    /// want to allocate.</param>
    /// <param name="dwSize">The size of the region of memory to allocate, in bytes.</param>
    /// <param name="flAllocationType">The type of memory allocation. This parameter must contain one of the following
    /// values: `MEM_COMMIT`, `MEM_RESERVE`, or `MEM_RESET`.</param>
    /// <param name="flProtect">The memory protection for the region of pages to be allocated. If the pages are being
    /// committed, you can specify any one of the memory protection constants.</param>
    /// <returns>The base address of the allocated region of pages.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr VirtualAllocEx(IntPtr hProcess, UIntPtr lpAddress, uint dwSize, uint flAllocationType,
        uint flProtect);
    
    /// <summary>
    /// Retrieves the address of an exported function or variable from the specified dynamic-link library (DLL).
    /// </summary>
    /// <param name="hModule">A handle to the DLL module that contains the function or variable. The LoadLibrary,
    /// LoadLibraryEx, LoadPackagedLibrary, or GetModuleHandle function returns this handle.</param>
    /// <param name="procName">The function or variable name, or the function's ordinal value. If this parameter is an
    /// ordinal value, it must be in the low-order word; the high-order word must be zero.</param>
    /// <returns>If the function succeeds, the return value is the address of the function or variable. If the function
    /// fails, the return value is NULL. To get extended error information, call GetLastError.</returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
    private static extern UIntPtr GetProcAddress(IntPtr hModule, string procName);
    
    /// <summary>
    /// Retrieves a module handle for the specified module. The module must have been loaded by the calling process.
    /// </summary>
    /// <param name="lpModuleName">The name of the loaded module (either a .dll or .exe file). If the file name
    /// extension is omitted, the default library extension .dll is appended. The file name string can include a
    /// trailing point character (.) to indicate that the module name has no extension. The string does not have to
    /// specify a path. When specifying a path, be sure to use backslashes (\), not forward slashes (/). The name is
    /// compared (case independently) to the names of modules currently mapped into the address space of the calling
    /// process.</param>
    /// <returns>If the function succeeds, the return value is a handle to the specified module. If the function fails,
    /// the return value is NULL. To get extended error information, call GetLastError.</returns>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
    
    /// <summary>
    /// Creates a thread that runs in the virtual address space of another process.
    /// </summary>
    /// <param name="hProcess">A handle to the process in which the thread is to be created. The handle must have the
    /// PROCESS_CREATE_THREAD, PROCESS_QUERY_INFORMATION, PROCESS_VM_OPERATION, PROCESS_VM_WRITE, and PROCESS_VM_READ
    /// access rights, and may fail without these rights on certain platforms.</param>
    /// <param name="lpThreadAttributes">A pointer to a SECURITY_ATTRIBUTES structure that specifies a security
    /// descriptor for the new thread and determines whether child processes can inherit the returned handle. If
    /// lpThreadAttributes is NULL, the thread gets a default security descriptor and the handle cannot be inherited.
    /// </param>
    /// <param name="dwStackSize">The initial size of the stack, in bytes. The system rounds this value to the nearest
    /// page. If this parameter is 0 (zero), the new thread uses the default size for the executable.</param>
    /// <param name="lpStartAddress">A pointer to the application-defined function of type LPTHREAD_START_ROUTINE to be
    /// executed by the thread and represents the starting address of the thread in the remote process. The function
    /// must exist in the process to be called.</param>
    /// <param name="lpParameter">A pointer to a variable to be passed to the thread function.</param>
    /// <param name="dwCreationFlags">The flags that control the creation of the thread.</param>
    /// <param name="lpThreadId">A pointer to a variable that receives the thread identifier. If this parameter is NULL,
    /// the thread identifier is not returned.</param>
    /// <returns>If the function succeeds, the return value is a handle to the new thread. If the function fails, the
    /// return value is NULL. To get extended error information, call GetLastError.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize,
        UIntPtr lpStartAddress, UIntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);
    
    /// <summary>
    /// Waits until the specified object is in the signaled state or the time-out interval elapses.
    /// </summary>
    /// <param name="hHandle">A handle to the object. For a list of the object types whose handles can be specified,
    /// see the following Remarks section. If this handle is closed while the wait is still pending, the function's
    /// behavior is undefined. The handle must have the SYNCHRONIZE access right.</param>
    /// <param name="dwMilliseconds">The time-out interval, in milliseconds. If a nonzero value is specified, the
    /// function waits until the object is signaled or the interval elapses. If dwMilliseconds is zero, the function
    /// does not enter a wait state if the object is not signaled; it always returns immediately. If dwMilliseconds is
    /// INFINITE, the function will return only when the object is signaled.</param>
    /// <returns>If the function succeeds, the return value indicates the event that caused the function to return. It
    /// can be one of the following values. WAIT_ABANDONED, WAIT_OBJECT_0, WAIT_TIMEOUT, WAIT_FAILED. To get extended
    /// error information, call GetLastError.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);
    
    /// <summary>
    /// Provides constants and methods related to the result of the <see cref="WaitForSingleObject"/> function.
    /// </summary>
    private static class WaitForSingleObjectResult
    {
        /// <summary>
        /// The specified object is a mutex object that was not released by the thread that owned the mutex object
        /// before the owning thread terminated. Ownership of the mutex object is granted to the calling thread and the
        /// mutex state is set to nonsignaled.
        /// </summary>
        public const uint Abandoned = 0x00000080;

        /// <summary>
        /// The state of the specified object is signaled.
        /// </summary>
        public const uint Signaled = 0x00000000;

        /// <summary>
        /// The time-out interval elapsed, and the object's state is nonsignaled.
        /// </summary>
        public const uint Timeout = 0x00000102;

        /// <summary>
        /// The function has failed. To get extended error information, call GetLastError.
        /// </summary>
        public const uint Failed = 0xFFFFFFFF;

        /// <summary>
        /// Determines if the given result indicates that the wait was successful.
        /// </summary>
        /// <param name="result">Result to test.</param>
        /// <returns>True if the result indicates success, false otherwise.</returns>
        public static bool IsSuccessful(uint result) => result == Signaled;
    }
    
    /// <summary>
    /// Closes an open object handle.
    /// </summary>
    /// <param name="hObject">A valid handle to an open object.</param>
    /// <returns>If the function succeeds, the return value is true. If the function fails, the return value is false.
    /// To get extended error information, call GetLastError.</returns>
    [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
    private static extern bool WinCloseHandle(IntPtr hObject);
    
    /// <summary>
    /// Releases a region of memory within the virtual address space of a specified process.
    /// </summary>
    /// <param name="hProcess">A handle to a process. The function frees memory within the virtual address space of this
    /// process. The handle must have the PROCESS_VM_OPERATION access right.</param>
    /// <param name="lpAddress">A pointer to the base address of the region of memory to be freed.</param>
    /// <param name="dwSize">The size of the region of memory to free, in bytes. If the dwFreeType parameter is
    /// MEM_RELEASE, this parameter must be 0 (zero). The function frees the entire region that is reserved in the
    /// initial allocation call to VirtualAllocEx.</param>
    /// <param name="dwFreeType">The type of free operation. This parameter can be one of the following values:
    /// MEM_DECOMMIT or MEM_RELEASE.</param>
    /// <returns>If the function succeeds, the return value is a nonzero value. If the function fails, the return value
    /// is 0 (zero). To get extended error information, call GetLastError.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualFreeEx(IntPtr hProcess, UIntPtr lpAddress, uint dwSize, uint dwFreeType);
    
    /// <summary>
    /// Reads memory in the given process.
    /// </summary>
    /// <param name="hProcess">A handle to the process with memory that is being read. The handle must have
    /// PROCESS_VM_READ access to the process.</param>
    /// <param name="lpBaseAddress">A pointer to the base address in the specified process from which to read. Before
    /// any data transfer occurs, the system verifies that all data in the base address and memory of the specified size
    /// is accessible for read access, and if it is not accessible the function fails.</param>
    /// <param name="lpBuffer">A pointer to a buffer that receives the contents from the address space of the specified
    /// process.</param>
    /// <param name="nSize">The number of bytes to be read from the specified process.</param>
    /// <param name="lpNumberOfBytesRead">A pointer to a variable that receives the number of bytes transferred into the
    /// specified buffer.</param>
    /// <returns>If the function succeeds, the return value is nonzero.
    /// If the function fails, the return value is 0.</returns>
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int ReadProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, [Out] byte[] lpBuffer,
        ulong nSize, out ulong lpNumberOfBytesRead);
    
    /// <summary>
    /// Changes the protection on a region of committed pages in the virtual address space of a specified process.
    /// </summary>
    /// <param name="hProcess">A handle to the process whose memory protection is to be changed. The handle must have
    /// the PROCESS_VM_OPERATION access right.</param>
    /// <param name="lpAddress">A pointer to the base address of the region of pages whose access protection attributes
    /// are to be changed.</param>
    /// <param name="dwSize">The size of the region whose access protection attributes are changed, in bytes.</param>
    /// <param name="flNewProtect">The memory protection option. This parameter can be one of the memory protection
    /// constants.</param>
    /// <param name="lpflOldProtect">A pointer to a variable that receives the previous access protection of the first
    /// page in the specified region of pages.</param>
    /// <returns>If the function succeeds, the return value is true. Otherwise, it will be false.</returns>
    [DllImport("kernel32.dll")]
    public static extern bool VirtualProtectEx(IntPtr hProcess, UIntPtr lpAddress,
        IntPtr dwSize, MemoryProtection flNewProtect, out MemoryProtection lpflOldProtect);
    
    /// <summary>
    /// Writes data to an area of memory in a specified process. The entire area to be written to must be accessible
    /// or the operation fails.
    /// </summary>
    /// <param name="hProcess">A handle to the process memory to be modified. The handle must have PROCESS_VM_WRITE and
    /// PROCESS_VM_OPERATION access to the process.</param>
    /// <param name="lpBaseAddress">A pointer to the base address in the specified process to which data is written.
    /// Before data transfer occurs, the system verifies that all data in the base address and memory of the specified
    /// size is accessible for write access, and if it is not accessible, the function fails.</param>
    /// <param name="lpBuffer">A pointer to the buffer that contains data to be written in the address space of the
    /// specified process.</param>
    /// <param name="nSize">The number of bytes to be written to the specified process.</param>
    /// <param name="lpNumberOfBytesWritten">A pointer to a variable that receives the number of bytes transferred into
    /// the specified process. This parameter is optional. If null, it will be ignored.</param>
    /// <returns>If the function succeeds, the return value is true. Otherwise, it will be false.</returns>
    [DllImport("kernel32.dll")]
    public static extern bool WriteProcessMemory(IntPtr hProcess, UIntPtr lpBaseAddress, byte[] lpBuffer, UIntPtr nSize,
        IntPtr lpNumberOfBytesWritten);
}