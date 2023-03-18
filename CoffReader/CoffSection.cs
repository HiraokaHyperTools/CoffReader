using System.Diagnostics;

namespace CoffReader;

[DebuggerDisplay("{Name}")]
public record CoffSection(
    string Name,
    uint PhysicalAddress,
    uint VirtualAddress,
    uint SectionSize,
    uint RawDataPosition,
    uint RelocationPosition,
    uint LineNumberPosition,
    ushort NumRelocations,
    ushort NumLineNumbers,
    uint Flags);
