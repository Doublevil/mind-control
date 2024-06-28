using System.Text;
using MindControl.Results;

namespace MindControl.Modules;

/// <summary>
/// Parser for PE headers.
/// </summary>
/// <param name="processMemory">Process memory instance to use to read memory.</param>
/// <param name="imageBaseAddress">Base address of the module to parse.</param>
internal class PeParser(ProcessMemory processMemory, UIntPtr imageBaseAddress)
{
    /// <summary>Offset of the PE Header start address, relative to the start of the module.</summary>
    private const int PeHeaderAddressOffset = 0x3C;
    
    /// <summary>
    /// Reads and parses the export table of the module, associating the names of the exported functions with their
    /// absolute addresses in the process memory.
    /// </summary>
    public Result<Dictionary<string, UIntPtr>, string> ReadExportTable()
    {
        var exportTable = new Dictionary<string, UIntPtr>();
        
        // Read the PE header address
        var peHeaderRva = processMemory.Read<uint>(imageBaseAddress + PeHeaderAddressOffset);
        if (peHeaderRva.IsFailure)
            return "Could not read the PE header address from the DOS header.";
        var peHeaderAddress = imageBaseAddress + peHeaderRva.Value;

        // Read the magic number from the Optional Header
        var optionalHeaderAddress = peHeaderAddress + 24; // Skip over the 20-byte File Header and 4-byte PE signature
        var magicNumber = processMemory.Read<ushort>(optionalHeaderAddress);
        if (magicNumber.IsFailure)
            return "Could not read the magic number.";
        
        bool? is64Bit = magicNumber.Value switch
        {
            0x10B => false, // 32-bit
            0x20B => true, // 64-bit
            _ => null
        };
        
        if (is64Bit == null)
            return $"Invalid magic number value: 0x{magicNumber.Value:X}.";
        
        // Read the export table address
        UIntPtr exportTableAddressPointer = peHeaderAddress + (UIntPtr)(is64Bit.Value ? 0x88 : 0x78);
        var exportTableAddressRva = processMemory.Read<uint>(exportTableAddressPointer);
        if (exportTableAddressRva.IsFailure)
            return "Could not read the export table address.";
        
        // Read the export table size
        var exportTableSize = processMemory.Read<uint>(exportTableAddressPointer + 4);
        if (exportTableSize.IsFailure)
            return "Could not read the export table size.";
        
        // Read the number of exported functions
        var exportTableAddress = imageBaseAddress + exportTableAddressRva.Value;
        var numberOfFunctions = processMemory.Read<uint>(exportTableAddress + 24);
        if (numberOfFunctions.IsFailure)
            return "Could not read the number of exported functions.";
        
        // Read the export name pointers table (ENPT)
        var enptBytes = ReadExportTableBytes(exportTableAddress + 32, numberOfFunctions.Value * 4);
        if (enptBytes == null)
            return "Could not read the export name pointers table.";
        
        // Read the export ordinal table (EOT)
        var eotBytes = ReadExportTableBytes(exportTableAddress + 36, numberOfFunctions.Value * 2);
        if (eotBytes == null)
            return "Could not read the export ordinal table.";
        
        // Read the export address table (EAT)
        var eatBytes = ReadExportTableBytes(exportTableAddress + 28, numberOfFunctions.Value * 4);
        if (eatBytes == null)
            return "Could not read the export address table.";
        
        for (int i = 0; i < numberOfFunctions.Value; i++)
        {
            // Read the function name using the ENPT
            var functionNameRva = BitConverter.ToUInt32(enptBytes, i * 4);
            var functionNameResult = processMemory.ReadRawString(imageBaseAddress + functionNameRva,
                Encoding.ASCII, 256);
            if (functionNameResult.IsFailure)
                continue;
            var functionName = functionNameResult.Value;

            // Read the ordinal of the function from the EOT
            var ordinal = BitConverter.ToUInt16(eotBytes, i * 2); 

            // Read the address of the function from the EAT using the ordinal
            var functionRva = BitConverter.ToUInt32(eatBytes, ordinal * 4);
            var functionAddress = imageBaseAddress + functionRva;
            
            exportTable.TryAdd(functionName, functionAddress);
        }

        return exportTable;
    }
    
    /// <summary>
    /// Reads the bytes of a table in the export table (ENPT, EOT or EAT).
    /// </summary>
    /// <param name="exportTableRvaPointer">Address of the field holding the RVA to the table to read.</param>
    /// <param name="exportTableSize">Size of the table to read.</param>
    /// <returns>The bytes of the table, or null if the table could not be read.</returns>
    private byte[]? ReadExportTableBytes(UIntPtr exportTableRvaPointer, uint exportTableSize)
    {
        var exportTableRva = processMemory.Read<uint>(exportTableRvaPointer);
        if (exportTableRva.IsFailure)
            return null;
        UIntPtr etAddress = imageBaseAddress + exportTableRva.Value;
        var etBytesResult = processMemory.ReadBytes(etAddress, exportTableSize);
        return etBytesResult.IsFailure ? null : etBytesResult.Value;
    }
}