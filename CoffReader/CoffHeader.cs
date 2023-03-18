namespace CoffReader;

internal record CoffHeader(
    ushort Magic,
    ushort NumSections,
    uint Timestamp,
    int SymbolTablePosition,
    uint NumSymbols,
    ushort OptionalHeaderSize,
    ushort Flags)
{
    public int SectionTablePosition => 20 + OptionalHeaderSize;
    public int StringTablePosition => (int) (SymbolTablePosition + 18 * NumSymbols);
}
