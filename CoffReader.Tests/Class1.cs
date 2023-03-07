using NUnit.Framework;

namespace CoffReader.Tests
{
    public class Class1
    {
        [Test]
        [TestCase(@"cygwin-x64/cyginvokezlibversion_dll_d000000.o")]
        [TestCase(@"cygwin-x64/cyginvokezlibversion_dll_d000001.o")]
        [TestCase(@"cygwin-x64/cyginvokezlibversion_dll_d000002.o")]
        [TestCase(@"cygwin-x64/cyginvokezlibversion_dll_d000003.o")]
        [TestCase(@"cygwin-x64/cyginvokezlibversion_dll_d000004.o")]
        [TestCase(@"cygwin-x64/invoke.cpp.o")]
        [TestCase(@"mingw-x86/invoke.cpp.obj")]
        [TestCase(@"mingw-x86/libinvokezlibversion_dll_d000000.o")]
        [TestCase(@"mingw-x86/libinvokezlibversion_dll_d000001.o")]
        [TestCase(@"mingw-x86/libinvokezlibversion_dll_d000002.o")]
        public void CoffParserTest(string objFile)
        {
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
        }

        private static string ResolvePath(string path) => Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "..",
            "..",
            "..",
            "Samples",
            path
        );
    }
}