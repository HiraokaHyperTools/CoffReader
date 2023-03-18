using System.Collections.Generic;

namespace CoffReader;

public class CoffParsed
{
    public ushort Magic { get; set; }
    public uint Timestamp { get; set; }
    public ushort Flags { get; set; }

    public List<CoffSymbol> Symbols { get; set; } = new List<CoffSymbol>();
    public List<CoffSection> Sections { get; set; } = new List<CoffSection>();

    public const ushort I386MAGIC = 0x14c;
    public const ushort AMD64MAGIC = 0x8664;
}
