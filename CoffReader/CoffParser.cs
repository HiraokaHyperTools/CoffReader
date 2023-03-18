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
    /// <summary>
    /// Gets or sets the default encoding for strings.
    /// </summary>
    public static Encoding DefaultEncoding { get; set; } = Encoding.UTF8;

    /// <summary>
    /// Gets or sets a value indicating whether the parser should use little-endian byte order.
    /// </summary>
    public static bool IsLittleEndian { get; set; } = BitConverter.IsLittleEndian;

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
        var (header, sections, symbols) = IsLittleEndian
            ? ParseLittleEndian(file, encoding)
            : ParseBigEndian(file, encoding);

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
        file.Slice((int) section.RawDataPosition, (int) section.SectionSize);

    private static CoffHeader ReadHeaderLittleEndian(ReadOnlySpan<byte> header) =>
        new CoffHeader(
            BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(0, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(2, 2)),
            BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(4, 4)),
            BinaryPrimitives.ReadInt32LittleEndian(header.Slice(8, 4)),
            BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(12, 4)),
            BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(16, 2)),
            BinaryPrimitives.ReadUInt16LittleEndian(header.Slice(18, 2)));

    private static CoffHeader ReadHeaderBigEndian(ReadOnlySpan<byte> header) =>
        new CoffHeader(
            BinaryPrimitives.ReadUInt16BigEndian(header.Slice(0, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(header.Slice(2, 2)),
            BinaryPrimitives.ReadUInt32BigEndian(header.Slice(4, 4)),
            BinaryPrimitives.ReadInt32BigEndian(header.Slice(8, 4)),
            BinaryPrimitives.ReadUInt32BigEndian(header.Slice(12, 4)),
            BinaryPrimitives.ReadUInt16BigEndian(header.Slice(16, 2)),
            BinaryPrimitives.ReadUInt16BigEndian(header.Slice(18, 2)));

    private static CoffSection ReadSectionLittleEndian(ReadOnlySpan<byte> secTabSpan, ReadOnlySpan<byte> stringTab, Encoding encoding)
        => new CoffSection(
            ReadSectionName(secTabSpan.Slice(0, 8), stringTab, encoding),
            BinaryPrimitives.ReadUInt32LittleEndian(secTabSpan.Slice(8)),
            BinaryPrimitives.ReadUInt32LittleEndian(secTabSpan.Slice(12)),
            BinaryPrimitives.ReadUInt32LittleEndian(secTabSpan.Slice(16)),
            BinaryPrimitives.ReadUInt32LittleEndian(secTabSpan.Slice(20)),
            BinaryPrimitives.ReadUInt32LittleEndian(secTabSpan.Slice(24)),
            BinaryPrimitives.ReadUInt32LittleEndian(secTabSpan.Slice(28)),
            BinaryPrimitives.ReadUInt16LittleEndian(secTabSpan.Slice(32)),
            BinaryPrimitives.ReadUInt16LittleEndian(secTabSpan.Slice(34)),
            BinaryPrimitives.ReadUInt32LittleEndian(secTabSpan.Slice(36)));

    private static CoffSection ReadSectionBigEndian(ReadOnlySpan<byte> secTabSpan, ReadOnlySpan<byte> stringTab, Encoding encoding)
        => new CoffSection(
            ReadSectionName(secTabSpan.Slice(0, 8), stringTab, encoding),
            BinaryPrimitives.ReadUInt32BigEndian(secTabSpan.Slice(8)),
            BinaryPrimitives.ReadUInt32BigEndian(secTabSpan.Slice(12)),
            BinaryPrimitives.ReadUInt32BigEndian(secTabSpan.Slice(16)),
            BinaryPrimitives.ReadUInt32BigEndian(secTabSpan.Slice(20)),
            BinaryPrimitives.ReadUInt32BigEndian(secTabSpan.Slice(24)),
            BinaryPrimitives.ReadUInt32BigEndian(secTabSpan.Slice(28)),
            BinaryPrimitives.ReadUInt16BigEndian(secTabSpan.Slice(32)),
            BinaryPrimitives.ReadUInt16BigEndian(secTabSpan.Slice(34)),
            BinaryPrimitives.ReadUInt32BigEndian(secTabSpan.Slice(36)));

    private static (CoffSymbol Symbol, int NumAux) ReadSymbolLittleEndian(ReadOnlySpan<byte> symTab, int index, ReadOnlySpan<byte> stringTab, Encoding encoding)
    {
        var symTabSpan = symTab.Slice(18 * index, 18);
        var name = ReadSymbolName(symTabSpan.Slice(0, 8), stringTab, encoding);
        var numAux = symTabSpan[17];
        var auxRecords = numAux == 0
            ? Array.Empty<byte[]>()
            : ReadAuxiliaryRecords(symTab, index + 1, numAux);

        var symbol = new CoffSymbol(
            name,
            BinaryPrimitives.ReadUInt32LittleEndian(symTabSpan.Slice(8)),
            BinaryPrimitives.ReadInt16LittleEndian(symTabSpan.Slice(12)),
            BinaryPrimitives.ReadUInt16LittleEndian(symTabSpan.Slice(14)),
            symTabSpan[16])
        {
            AuxiliaryRecords = auxRecords,
        };

        return (symbol, numAux);
    }

    private static (CoffSymbol Symbol, int NumAux) ReadSymbolBigEndian(ReadOnlySpan<byte> symTab, int index, ReadOnlySpan<byte> stringTab, Encoding encoding)
    {
        var symTabSpan = symTab.Slice(18 * index, 18);
        var name = ReadSymbolName(symTabSpan.Slice(0, 8), stringTab, encoding);
        var numAux = symTabSpan[17];
        var auxRecords = numAux == 0
            ? Array.Empty<byte[]>()
            : ReadAuxiliaryRecords(symTab, index + 1, numAux);

        var symbol = new CoffSymbol(
            name,
            BinaryPrimitives.ReadUInt32BigEndian(symTabSpan.Slice(8)),
            BinaryPrimitives.ReadInt16BigEndian(symTabSpan.Slice(12)),
            BinaryPrimitives.ReadUInt16BigEndian(symTabSpan.Slice(14)),
            symTabSpan[16])
        {
            AuxiliaryRecords = auxRecords,
        };

        return (symbol, numAux);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<byte> GetStringTableLittleEndian(ReadOnlySpan<byte> file, CoffHeader header)
    {
        var ofsStringTab = header.StringTablePosition;
        var sizeStringTab = BinaryPrimitives.ReadInt32LittleEndian(file.Slice(ofsStringTab, 4));
        return file.Slice(ofsStringTab, sizeStringTab);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<byte> GetStringTableBigEndian(ReadOnlySpan<byte> file, CoffHeader header)
    {
        var ofsStringTab = header.StringTablePosition;
        var sizeStringTab = BinaryPrimitives.ReadInt32BigEndian(file.Slice(ofsStringTab, 4));
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IReadOnlyList<byte[]> ReadAuxiliaryRecords(ReadOnlySpan<byte> symTab, int startIndex, int count)
    {
        var result = new List<byte[]>(count);
        var endIndex = startIndex + count;
        for (var i = startIndex; i != endIndex; ++i)
        {
            var auxRecord = symTab.Slice(i * 18, 18);
            result.Add(auxRecord.ToArray());
        }

        return result;
    }

    /// <summary>
    /// Parses the COFF file using the given encoding for strings.
    /// </summary>
    /// <param name="file">The file data to parse.</param>
    /// <param name="encoding">The encoding for the strings.</param>
    /// <returns>The parsed file.</returns>
    private static (CoffHeader Header, IReadOnlyList<CoffSection> Sections, IReadOnlyList<CoffSymbol> Symbols)
        ParseLittleEndian(ReadOnlySpan<byte> file, Encoding encoding)
    {
        var header = ReadHeaderLittleEndian(file.Slice(0, 20));
        var stringTab = GetStringTableLittleEndian(file, header);

        var sections = new List<CoffSection>();
        for (var idx = 0; idx < header.NumSections; idx++)
        {
            var secTabSpan = file.Slice(header.SectionTablePosition + 40 * idx, 40);
            sections.Add(ReadSectionLittleEndian(secTabSpan, stringTab, encoding));
        }

        var symbols = new List<CoffSymbol>();
        var symTab = file.Slice(header.SymbolTablePosition, (int) (18 * header.NumSymbols));
        for (var idx = 0; idx < header.NumSymbols; idx++)
        {
            var (symbol, numAux) = ReadSymbolLittleEndian(symTab, idx, stringTab, encoding);
            symbols.Add(symbol);

            // Skip the auxiliary records.
            idx += numAux;
        }

        return (header, sections, symbols);
    }

    /// <summary>
    /// Parses the COFF file using the given encoding for strings.
    /// </summary>
    /// <param name="file">The file data to parse.</param>
    /// <param name="encoding">The encoding for the strings.</param>
    /// <returns>The parsed file.</returns>
    private static (CoffHeader Header, IReadOnlyList<CoffSection> Sections, IReadOnlyList<CoffSymbol> Symbols)
        ParseBigEndian(ReadOnlySpan<byte> file, Encoding encoding)
    {
        var header = ReadHeaderBigEndian(file.Slice(0, 20));
        var stringTab = GetStringTableBigEndian(file, header);

        var sections = new List<CoffSection>();
        for (var idx = 0; idx < header.NumSections; idx++)
        {
            var secTabSpan = file.Slice(header.SectionTablePosition + 40 * idx, 40);
            sections.Add(ReadSectionBigEndian(secTabSpan, stringTab, encoding));
        }

        var symbols = new List<CoffSymbol>();
        var symTab = file.Slice(header.SymbolTablePosition, (int) (18 * header.NumSymbols));
        for (var idx = 0; idx < header.NumSymbols; idx++)
        {
            var (symbol, numAux) = ReadSymbolBigEndian(symTab, idx, stringTab, encoding);
            symbols.Add(symbol);

            // Skip the auxiliary records.
            idx += numAux;
        }

        return (header, sections, symbols);
    }
}
