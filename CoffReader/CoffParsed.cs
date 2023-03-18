using System;
using System.Collections.Generic;

namespace CoffReader;

public record CoffParsed(ushort Magic, uint Timestamp, ushort Flags)
{
    public IReadOnlyList<CoffSymbol> Symbols { get; init; } = Array.Empty<CoffSymbol>();
    public IReadOnlyList<CoffSection> Sections { get; init; } = Array.Empty<CoffSection>();

    public const ushort I386MAGIC = 0x14c;
    public const ushort AMD64MAGIC = 0x8664;
}
