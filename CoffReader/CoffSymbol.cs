namespace CoffReader;

public class CoffSymbol
{
    public string Name { get; set; }
    public uint Value { get; set; }
    public short SectionNumber { get; set; }
    public ushort SymbolType { get; set; }
    public byte StorageClass { get; set; }
    public byte NumAux { get; internal set; }

    public override string ToString() => $"{Name}";
}
