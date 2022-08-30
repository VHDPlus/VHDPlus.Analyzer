using System.Diagnostics;
using System.IO;
using System.Linq;
using VHDPlus.Analyzer.Checks;
using VHDPlus.Analyzer.Elements;
using VHDPlus.Analyzer.Info;
using Xunit;
using Xunit.Abstractions;

namespace VHDPlus.Analyzer.Tests
{
    public class AnalyzerTests
    {
        private static readonly string AssetsFolder = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Assets");
        private readonly ITestOutputHelper _output;

        public AnalyzerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        private AnalyzerContext RunAnalyzer(AnalyzerMode mode, string path)
        {
            var stopWatch = new Stopwatch();
            var text = File.ReadAllText(path);
            stopWatch.Start();
            var result = Analyzer.Analyze(path, text, mode);
            stopWatch.Stop();
            _output.WriteLine($"Analyze took {stopWatch.ElapsedMilliseconds.ToString()}ms");
            return result;
        }
        
        private void Analyzer2Test(AnalyzerMode mode, string path)
        {
            var result = RunAnalyzer(mode,path);
            
            foreach (var diag in result.Diagnostics)
            {
                _output.WriteLine("{0} {1}:{2}", diag.Message, diag.StartLine.ToString(), diag.StartCol.ToString());
            }
            _output.WriteLine("---------");
            
            foreach (var segment in result.TopSegment.Children)
            {
                Helper.PrintSegment(segment, _output);
            }
        }

        [Fact]
        public void Analyzer2TestIndexing()
        {
            Analyzer2Test(AnalyzerMode.Indexing,Path.Combine(AssetsFolder, "Debug.vhdp"));
        }
        
        [Fact]
        public void Analyzer2TestFull()
        {
            Analyzer2Test(AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check,Path.Combine(AssetsFolder, "Debug.vhdp"));
        }
        
        [Fact]
        public void Analyzer2TestFullVhdl()
        {
            Analyzer2Test(AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check,Path.Combine(AssetsFolder, "Random_Number.vhd"));
        }

        [Fact]
        public void Analyzer2ReadVariables()
        {
            var result = RunAnalyzer(AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check, Path.Combine(AssetsFolder, "OV5647_Camera.vhdp"));

            foreach (var s in result.AvailableTypes)
            {
                _output.WriteLine(s.Key + "\n--------");
                if(s.Value is CustomDefinedRecord record)
                    foreach (var variable in record.Variables)
                    {
                        _output.WriteLine($"{variable.Value.VariableType} {variable.Value.Name} : {variable.Value.DataType}");
                    }
            }
        }
        
        
        [Fact]
        public void CrawlTest()
        {
            var result = RunAnalyzer(AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check,Path.Combine(AssetsFolder, "Debug.vhdp"));
            SegmentCrawler.GetPairs(result.TopSegment, (parent, child, parameter, thread) =>
            {
                _output.WriteLine($"Parent: {parent}; Child: {child}; Parameter: {parameter}; Thread: {thread}");
            });
        }

        [Fact]
        public void SegmentPrintTest()
        {
            var result = RunAnalyzer(AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check,Path.Combine(AssetsFolder, "Debug.vhdp"));
            _output.WriteLine(PrintSegment.Convert(result.TopSegment));
        }
        
        [Fact]
        public void AnalyzerGetSegmentFromOffsetTest()
        {
            var text = File.ReadAllText(Path.Combine(AssetsFolder, "OV5647_Camera.vhdp"));
            var result = Analyzer.Analyze("",text, AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check);

            var segment = AnalyzerHelper.GetSegmentFromOffset(result,  5217);
            
            Assert.NotNull(segment);
            Assert.Equal(SegmentType.DataVariable, segment?.SegmentType);
        }
        
        [Fact]
        public void AnalyzerParameterTests()
        {
            Assert.Empty(Analyzer.Analyze("","Component a (){SIGNAL LED : INTEGER := (12) + (0 + (((1/10))/10)/10);}", AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check).Diagnostics);
            Assert.Empty(Analyzer.Analyze("","Component a (){SIGNAL LED : INTEGER := ((12) + (0 + (((1/10))/10)/10));}", AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check).Diagnostics);
            Assert.Empty(Analyzer.Analyze("","Component a (){SIGNAL LED : INTEGER := ((5) => (((5)/30)));}", AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check).Diagnostics);
            Assert.Empty(Analyzer.Analyze("","Component a (){SIGNAL LED : BOOLEAN := ((true) AND false);}", AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check).Diagnostics);
        }
        
        [Fact]
        public void OperatorCheckTests()
        {
            Assert.Empty(Analyzer.Analyze("","Component a (){Signal a : STD_LOGIC; a <= '1';}", AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check).Diagnostics);
            Assert.Collection(Analyzer.Analyze("","Component a (){Signal a : STD_LOGIC; a := '1';}", AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check)
                .Diagnostics, x => Assert.StartsWith("Invalid Operator", x.Message));
            Assert.Empty(Analyzer.Analyze("","Component a (){Process(){Variable a : STD_LOGIC; a := '1';}}", AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check).Diagnostics);
            Assert.Collection(Analyzer.Analyze("","Component a (){Process(){Variable a : STD_LOGIC; a <= '1';}}", AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check)
                .Diagnostics, x => Assert.StartsWith("Invalid Operator", x.Message));
        }

        [Fact]
        public void TypeCheckTest()
        {
            var result = RunAnalyzer(AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check,Path.Combine(AssetsFolder, "TypeCheck.vhdp"));
            foreach (var diag in result.Diagnostics)
            {
                _output.WriteLine("{0} {1}:{2}", diag.Message, diag.StartLine.ToString(), diag.StartCol.ToString());
            }
            Assert.Empty(result.Diagnostics);
            
            var result2 = RunAnalyzer(AnalyzerMode.Indexing | AnalyzerMode.Resolve | AnalyzerMode.Check,Path.Combine(AssetsFolder, "TypeCheckInvalid.vhdp"));
            foreach (var diag in result2.Diagnostics)
            {
                _output.WriteLine("{0} {1}:{2}", diag.Message, diag.StartLine.ToString(), diag.StartCol.ToString());
            }
            Assert.Single(result2.Diagnostics);
        }
    }
}
