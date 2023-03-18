using System;
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
        var header = file.Slice(0, 20).ToArray();
        var f_magic = BitConverter.ToUInt16(header, 0);
        var f_nscns = BitConverter.ToUInt16(header, 2);
        var f_timdat = BitConverter.ToUInt32(header, 4);
        var f_symptr = BitConverter.ToUInt32(header, 8);
        var f_nsyms = BitConverter.ToUInt32(header, 12);
        var f_opthdr = BitConverter.ToUInt16(header, 16);
        var f_flags = BitConverter.ToUInt16(header, 18);

        var parsed = new CoffParsed
        {
            Timestamp = f_timdat,
            Magic = f_magic,
            Flags = f_flags,
        };

        {
            for (int idx = 0; idx < f_nscns; idx++)
            {
                var secTab = file.Slice(Convert.ToInt32(20 + 40 * idx), 40).ToArray();

                parsed.Sections.Add(
                    new CoffSection
                    {
                        Name = _raw.GetString(secTab, 0, 8).Split('\0').First(),
                        PhysicalAddress = BitConverter.ToUInt32(secTab, 8),
                        VirtualAddress = BitConverter.ToUInt32(secTab, 12),
                        SectionSize = BitConverter.ToUInt32(secTab, 16),
                        RawDataPosition = BitConverter.ToUInt32(secTab, 20),
                        RelocationPosition = BitConverter.ToUInt32(secTab, 24),
                        LineNumberPosition = BitConverter.ToUInt32(secTab, 28),
                        NumRelocations = BitConverter.ToUInt16(secTab, 32),
                        NumLineNumbers = BitConverter.ToUInt16(secTab, 34),
                        Flags = BitConverter.ToUInt32(secTab, 36),
                    }
                );
            }
        }

        {
            var ofsStringTab = Convert.ToInt32(f_symptr + 18 * f_nsyms);
            var sizeStringTab = BitConverter.ToInt32(file.Slice(ofsStringTab, 4).ToArray(), 0);
            var stringTab = _raw.GetString(
                file.Slice(ofsStringTab, Convert.ToInt32(sizeStringTab))
                    .ToArray()
            );
            for (int idx = 0; idx < f_nsyms; idx++)
            {
                var symTab = file.Slice(Convert.ToInt32(f_symptr + 18 * idx), 18).ToArray();
                var nameIsRef = BitConverter.ToInt32(symTab, 0) == 0;
                var nameRef = BitConverter.ToInt32(symTab, 0 + 4);
                var name = nameIsRef
                    ? ((nameRef == 0)
                        ? ""
                        : stringTab.Substring(nameRef).Split('\0').First()
                    )
                    : _raw.GetString(symTab, 0, 8).Split('\0').First();

                parsed.Symbols.Add(
                    new CoffSymbol
                    {
                        Name = name,
                        Value = BitConverter.ToUInt32(symTab, 8),
                        SectionNumber = BitConverter.ToInt16(symTab, 12),
                        SymbolType = BitConverter.ToUInt16(symTab, 14),
                        StorageClass = symTab[16],
                        NumAux = symTab[17],
                    }
                );
            }
        }

        return parsed;
    }

    public static Span<byte> ReadRawData(Span<byte> file, CoffSection section) =>
        file.Slice(Convert.ToInt32(section.RawDataPosition), Convert.ToInt32(section.SectionSize));
}
