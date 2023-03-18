namespace CoffReader;

public class CoffSection
{
    public string Name { get; set; }
    public uint PhysicalAddress { get; set; }
    public uint VirtualAddress { get; set; }
    public uint SectionSize { get; set; }
    public uint RawDataPosition { get; set; }
    public uint RelocationPosition { get; set; }
    public uint LineNumberPosition { get; set; }
    public ushort NumRelocations { get; set; }
    public ushort NumLineNumbers { get; set; }
    public uint Flags { get; set; }

    public override string ToString() => $"{Name}";
}
