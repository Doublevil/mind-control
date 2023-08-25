using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
#pragma warning disable CS0649

namespace MindControl.Native;

/// <summary>
/// Contains DllImports for Windows functions required internally by other components.
/// </summary>
public class Win32Service : IOperatingSystemService
{
    #region Imports
    
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
    /// Contains information about a range of pages in the virtual address space of a process.
    /// 32-bit variant, without the alignment values.
    /// </summary>
    private struct MEMORY_BASIC_INFORMATION32
    {
        /// <summary>
        /// A pointer to the base address of the region of pages.
        /// </summary>
        public UIntPtr BaseAddress;
        
        /// <summary>
        /// A pointer to the base address of a range of pages allocated by the VirtualAlloc function. The page pointed
        /// to by the BaseAddress member is contained within this allocation range.
        /// </summary>
        public UIntPtr AllocationBase;
        
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
        /// The state of the pages in the region: committed (0x1000), free (0x10000), or reserved (0x2000).
        /// </summary>
        public uint State;
        
        /// <summary>
        /// The access protection of the pages in the region. This member is one of the values listed for the
        /// AllocationProtect member.
        /// </summary>
        public uint Protect;
        
        /// <summary>
        /// The type of pages in the region: image (0x1000000), mapped (0x40000) or private (0x20000).
        /// </summary>
        public uint Type;
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
        out MEMORY_BASIC_INFORMATION32 lpBuffer, UIntPtr dwLength);

    /// <summary>
    /// Contains information about a range of pages in the virtual address space of a process.
    /// 64-bit variant, with the alignment values.
    /// </summary>
    private struct MEMORY_BASIC_INFORMATION64
    {
        /// <summary>
        /// A pointer to the base address of the region of pages.
        /// </summary>
        public UIntPtr BaseAddress;
        
        /// <summary>
        /// A pointer to the base address of a range of pages allocated by the VirtualAlloc function. The page pointed
        /// to by the BaseAddress member is contained within this allocation range.
        /// </summary>
        public UIntPtr AllocationBase;
        
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
        /// The state of the pages in the region: committed (0x1000), free (0x10000), or reserved (0x2000).
        /// </summary>
        public uint State;
        
        /// <summary>
        /// The access protection of the pages in the region. This member is one of the values listed for the
        /// AllocationProtect member.
        /// </summary>
        public uint Protect;
        
        /// <summary>
        /// The type of pages in the region: image (0x1000000), mapped (0x40000) or private (0x20000).
        /// </summary>
        public uint Type;
        
        /// <summary>
        /// Second alignment value, specific to the 64-bit variant of this structure.
        /// </summary>
        public uint __alignment2;
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
        out MEMORY_BASIC_INFORMATION64 lpBuffer, UIntPtr dwLength);
    
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
    
    #endregion

    /// <summary>
    /// Opens the process with the given identifier, in a way that allows memory manipulation.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>Handle of the opened process.</returns>
    public IntPtr OpenProcess(int pid)
    {
        var handle = OpenProcess(0x1F0FFF, true, pid);
        if (handle == IntPtr.Zero)
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.

        return handle;
    }

    /// <summary>
    /// Returns a value indicating if the process with the given identifier is a 64-bit process or not.
    /// </summary>
    /// <param name="pid">Identifier of the target process.</param>
    /// <returns>True if the process is 64-bits, false otherwise.</returns>
    public bool IsProcess64Bits(int pid)
    {
        var process = Process.GetProcessById(pid);
        if (process == null)
            throw new ArgumentException($"Process {pid} was not found.");
        
        if (!IsWow64Process(process.Handle, out bool isWow64))
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.

        bool isSystem64Bits = IntPtr.Size == 8;
        
        // Process is 64 bits if we are running a 64-bits system and the process is NOT in wow64.
        return !isWow64 && isSystem64Bits;
    }

    /// <summary>
    /// Reads a targeted range of the memory of a specified process.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_READ access.</param>
    /// <param name="baseAddress">Starting address of the memory range to read.</param>
    /// <param name="length">Length of the memory range to read.</param>
    /// <returns>An array of bytes containing the data read from the memory.</returns>
    public byte[]? ReadProcessMemory(IntPtr processHandle, UIntPtr baseAddress, ulong length)
    {
        if (processHandle == IntPtr.Zero)
            throw new ArgumentException("The process handle is invalid (zero pointer).", nameof(processHandle));
        
        var result = new byte[length];
        int returnValue = ReadProcessMemory(processHandle, baseAddress, result, length, out _);

        if (returnValue == 0)
        {
            int errorCode = Marshal.GetLastWin32Error();
            
            // ERROR_PARTIAL_COPY (299): Generic error that is raised when the address isn't valid for whatever reason.
            // This error is quite generic and does not really allow users to identify what's wrong.
            // In order to simplify error handling by a significant margin and also preserve performance, we will
            // not throw when getting this particular error code.
            if (errorCode == 299)
                return null;
            
            // ERROR_NOACCESS (998): Error raised when the memory we are trying to read is protected for whatever
            // reason. Since this can be due to trying to access an invalid address, for the same reasons as noted
            // above (error 299), we will not throw. This behaviour might change or be configurable in later releases.
            if (errorCode == 998)
                return null;

            // In other cases, throw.
            throw new Win32Exception(errorCode);
        }
        
        return result;
    }
    
    /// <summary>
    /// Overwrites the memory protection of the page that the given address is part of.
    /// Returns the memory protection that was effective on the page before being changed.
    /// </summary>
    /// <param name="processHandle">Handle of the target process.
    /// The handle must have PROCESS_VM_OPERATION access.</param>
    /// <param name="is64Bits">A boolean indicating if the target process is 64 bits or not.</param>
    /// <param name="targetAddress">An address in the target page.</param>
    /// <param name="newProtection">New protection value for the page.</param>
    /// <returns>The memory protection value that was effective on the page before being changed.</returns>
    public MemoryProtection ReadAndOverwriteProtection(IntPtr processHandle, bool is64Bits, UIntPtr targetAddress,
        MemoryProtection newProtection)
    {
        if (processHandle == IntPtr.Zero)
            throw new ArgumentException("The process handle is invalid (zero pointer).", nameof(processHandle));
        if (targetAddress == UIntPtr.Zero)
            throw new ArgumentOutOfRangeException(nameof(targetAddress),"The target address cannot be a zero pointer.");

        bool result = VirtualProtectEx(processHandle, targetAddress, (IntPtr)(is64Bits ? 8 : 4), newProtection,
            out var previousProtection);

        if (!result)
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.

        return previousProtection;
    }

    /// <summary>
    /// Writes the given bytes into the memory of the specified process, at the target address.
    /// </summary>
    /// <param name="processHandle">Handle of the target process. The handle must have PROCESS_VM_WRITE and
    /// PROCESS_VM_OPERATION access.</param>
    /// <param name="targetAddress">Base address in the memory of the process to which data will be written.</param>
    /// <param name="value">Byte array to write in the memory. It is assumed that the entire array will be
    /// written, unless a size is specified.</param>
    /// <param name="size">Specify this value if you only want to write part of the value array in memory.
    /// This parameter is useful when using buffer byte arrays. Leave it to null to use the entire array.</param>
    public void WriteProcessMemory(IntPtr processHandle, UIntPtr targetAddress, byte[] value, int? size = null)
    {
        if (processHandle == IntPtr.Zero)
            throw new ArgumentException("The process handle is invalid (zero pointer).", nameof(processHandle));
        if (targetAddress == UIntPtr.Zero)
            throw new ArgumentOutOfRangeException(nameof(targetAddress),"The target address cannot be a zero pointer.");
        if (size != null && size.Value > value.Length)
            throw new ArgumentOutOfRangeException(nameof(size),"The size cannot exceed the length of the value array.");

        bool result = WriteProcessMemory(processHandle, targetAddress, value,
            (UIntPtr)(size ?? value.Length), IntPtr.Zero);
        
        if (!result)
            throw new Win32Exception(); // This constructor does all the job to retrieve the error by itself.
    }
}