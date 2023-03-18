using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace CoffReader;

/// <summary>
/// COFF parser
/// </summary>
/// <see cref="http://delorie.com/djgpp/doc/coff/"/>
public class CoffParser
{
    public static Encoding DefaultEncoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// Parses the COFF file using the default encoding for strings.
    /// </summary>
    /// <param name="file">The file data to parse.</param>
    /// <returns>The parsed file.</returns>
    public static CoffParsed Parse(ReadOnlySpan<byte> file)
    {
        return Parse(file, DefaultEncoding);
    }

    /// <summary>
    /// Parses the COFF file using the given encoding for strings.
    /// </summary>
    /// <param name="file">The file data to parse.</param>
    /// <param name="encoding">The encoding for the strings.</param>
    /// <returns>The parsed file.</returns>
    public static CoffParsed Parse(ReadOnlySpan<byte> file, Encoding encoding)
    {
        var header = ReadHeader(file.Slice(0, 20));
        var stringTab = GetStringTable(file, header);

        var sections = new List<CoffSection>();
        {
            for (int idx = 0; idx < header.NumSections; idx++)
            {
                var secTabSpan = file.Slice(Convert.ToInt32(header.SectionTablePosition + 40 * idx), 40);
                var secTab = secTabSpan.ToArray();

                sections.Add(
                    new CoffSection(
                        ReadSectionName(secTabSpan.Slice(0, 8), stringTab, encoding),
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
            for (int idx = 0; idx < header.NumSymbols; idx++)
            {
                var symTabSpan = file.Slice(Convert.ToInt32(header.SymbolTablePosition + 18 * idx), 18);
                var symTab = symTabSpan.ToArray();
                var name = ReadSymbolName(symTabSpan.Slice(0, 8), stringTab, encoding);

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

    /// <summary>
    /// Returns the raw data for the given section.
    /// </summary>
    /// <param name="file">The COFF file.</param>
    /// <param name="section">The section to get the data for.</param>
    /// <returns>The section data.</returns>
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<byte> GetStringTable(ReadOnlySpan<byte> file, CoffHeader header)
    {
        var ofsStringTab = header.StringTablePosition;
        var sizeStringTab = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(ofsStringTab, 4));
        return file.Slice(ofsStringTab, sizeStringTab);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe string ReadNulTerminatedString(ReadOnlySpan<byte> ptr, Encoding encoding)
    {
        var nulIndex = ptr.IndexOf<byte>(0);
        var length = nulIndex == -1 ? ptr.Length : nulIndex;
        fixed (byte* pb = ptr)
        {
            return encoding.GetString(pb, length);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ParseUInt32(ReadOnlySpan<byte> ptr)
    {
        var result = 0U;
        var length = ptr.Length;
        for (var i = 0; i != length; ++i)
        {
            var b = ptr[i];
            if (b < '0' || b > '9')
            {
                break;
            }

            var c = (uint) (ptr[i] - '0');
            result = result * 10 + c;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ReadSymbolName(ReadOnlySpan<byte> name, ReadOnlySpan<byte> stringTab, Encoding encoding)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(name.Slice(0, 4)) == 0
            ? BinaryPrimitives.ReadInt32LittleEndian(name.Slice(4, 4)) switch
            {
                0 => string.Empty,
                var nameRef => ReadNulTerminatedString(stringTab.Slice(nameRef), encoding),
            }
            : ReadNulTerminatedString(name, encoding);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ReadSectionName(ReadOnlySpan<byte> name, ReadOnlySpan<byte> stringTab, Encoding encoding) =>
        name[0] == '/'
            ? ReadNulTerminatedString(stringTab.Slice((int) ParseUInt32(name.Slice(1))), encoding)
            : ReadNulTerminatedString(name, encoding);
}
