using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CoffReader;

[DebuggerDisplay("{Name}")]
public record CoffSymbol(
    string Name,
    uint Value,
    short SectionNumber,
    ushort SymbolType,
    byte StorageClass)
{
    [Obsolete("Use AuxiliaryRecords.Count instead.")]
    public int NumAux => AuxiliaryRecords.Count;

    /// <summary>
    /// Gets the auxiliary records.
    /// </summary>
    public IReadOnlyList<byte[]> AuxiliaryRecords { get; init; } = Array.Empty<byte[]>();
}
