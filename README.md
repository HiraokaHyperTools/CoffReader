# HiraokaHyperTools.CoffReader

[![Nuget](https://img.shields.io/nuget/v/HiraokaHyperTools.CoffReader)](https://www.nuget.org/packages/HiraokaHyperTools.CoffReader)

This will parse `.obj` file built by modern Visual Studio C++ compiler.

## Enumeration usage

```cs
var objFileBytes = File.ReadAllBytes(ResolvePath(objFile));

var parsed = CoffParser.Parse(objFileBytes);

parsed.Sections
    .ToList()
    .ForEach(
        it => Console.WriteLine($"{it.Name,-8} {it.Flags:X8} {it.RawDataPosition,6} {it.SectionSize,6} ")
    );

Console.WriteLine();

parsed.Symbols
    .ToList()
    .ForEach(
        it => Console.WriteLine($"0x{it.Value:X8} {it.SectionNumber,3} {it.SymbolType,2} {it.StorageClass} {it.NumAux} {it.Name} ")
    );
```

`cygwin-x64/cyginvokezlibversion_dll_d000001.o`

```txt
.text    60300020    220      8 
.idata$7 C0300000    228      4 
.idata$5 C0300000    232      8 
.idata$4 C0300000    240      8 
.idata$6 C0300000    248     28 

0x00000000   1  0 3 0 .text 
0x00000000   2  0 3 0 .idata$7 
0x00000000   3  0 3 0 .idata$5 
0x00000000   4  0 3 0 .idata$4 
0x00000000   5  0 3 0 .idata$6 
0x00000000   1  0 2 0 _Z17invokezlibversionv 
0x00000000   3  0 2 0 __imp__Z17invokezlibversionv 
0x00000000   0  0 2 0 _head_cyginvokezlibversion_dll 
```

## Windows import library detection usage

```cs
var objData = ArFileParser.ReadData(arFileData, arEntry);

var obj = CoffParser.Parse(objData);
if (obj.Magic == CoffParsed.I386MAGIC || obj.Magic == CoffParsed.AMD64MAGIC)
{
    foreach (var section in obj.Sections.Where(section => section.Name == ".idata$7"))
    {
        var idataV7 = CoffParser.ReadRawData(objData, section);
        list.AddRange(
            Encoding.Latin1.GetString(idataV7)
                .Split('\0')
                .Where(it => it.Length != 0)
        );
    }
}
else
{
    _logger.LogWarning($"Invalid COFF magic 0x{obj.Magic:X4} of `{arEntry.FileName}` in `{libFile}`");
}
```
