using System.Diagnostics;

namespace CoffReader;

[DebuggerDisplay("{Name}")]
public record CoffSymbol(
    string Name,
    uint Value,
    short SectionNumber,
    ushort SymbolType,
    byte StorageClass,
    byte NumAux);
