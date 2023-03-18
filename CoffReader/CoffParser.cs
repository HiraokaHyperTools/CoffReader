using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CoffReader;

/// <summary>
/// COFF parser
/// </summary>
/// <see cref="http://delorie.com/djgpp/doc/coff/"/>
public class CoffParser
{
    private static readonly Encoding _raw = Encoding.GetEncoding("latin1");

    public static CoffParsed Parse(Span<byte> file)
    {
        var header = ReadHeader(file.Slice(0, 20));

        var sections = new List<CoffSection>();
        {
            for (int idx = 0; idx < header.NumSections; idx++)
            {
                var secTab = file.Slice(Convert.ToInt32(header.SectionTablePosition + 40 * idx), 40).ToArray();

                sections.Add(
                    new CoffSection(
                        _raw.GetString(secTab, 0, 8).Split('\0').First(),
                        BitConverter.ToUInt32(secTab, 8),
                        BitConverter.ToUInt32(secTab, 12),
                        BitConverter.ToUInt32(secTab, 16),
                        BitConverter.ToUInt32(secTab, 20),
                        BitConverter.ToUInt32(secTab, 24),
                        BitConverter.ToUInt32(secTab, 28),
                        BitConverter.ToUInt16(secTab, 32),
                        BitConverter.ToUInt16(secTab, 34),
                        BitConverter.ToUInt32(secTab, 36)));
            }
        }

        var symbols = new List<CoffSymbol>();
        {
            var ofsStringTab = Convert.ToInt32(header.SymbolTablePosition + 18 * header.NumSymbols);
            var sizeStringTab = BitConverter.ToInt32(file.Slice(ofsStringTab, 4).ToArray(), 0);
            var stringTab = _raw.GetString(
                file.Slice(ofsStringTab, Convert.ToInt32(sizeStringTab))
                    .ToArray()
            );
            for (int idx = 0; idx < header.NumSymbols; idx++)
            {
                var symTab = file.Slice(Convert.ToInt32(header.SymbolTablePosition + 18 * idx), 18).ToArray();
                var nameIsRef = BitConverter.ToInt32(symTab, 0) == 0;
                var nameRef = BitConverter.ToInt32(symTab, 0 + 4);
                var name = nameIsRef
                    ? ((nameRef == 0)
                        ? ""
                        : stringTab.Substring(nameRef).Split('\0').First()
                    )
                    : _raw.GetString(symTab, 0, 8).Split('\0').First();

                symbols.Add(
                    new CoffSymbol(
                        name,
                        BitConverter.ToUInt32(symTab, 8),
                        BitConverter.ToInt16(symTab, 12),
                        BitConverter.ToUInt16(symTab, 14),
                        symTab[16],
                        symTab[17]));
            }
        }

        var parsed = new CoffParsed(
            header.Magic,
            header.Timestamp,
            header.Flags)
        {
            Sections = sections,
            Symbols = symbols,
        };

        return parsed;
    }

    public static Span<byte> ReadRawData(Span<byte> file, CoffSection section) =>
        file.Slice(Convert.ToInt32(section.RawDataPosition), Convert.ToInt32(section.SectionSize));

    private static CoffHeader ReadHeader(ReadOnlySpan<byte> header)
    {
        var magic = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(0, 2));
        var nscns = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(2, 2));
        var timdat = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(4, 4));
        var symptr = BinaryPrimitives.ReadInt32LittleEndian(header.Slice(8, 4));
        var nsyms = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(12, 4));
        var opthdr = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(16, 2));
        var flags = BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(18, 2));
        return new CoffHeader(
            magic,
            nscns,
            timdat,
            symptr,
            nsyms,
            opthdr,
            flags);
    }
}
