using System.Diagnostics;
using System.IO;
using System.Linq;
using VHDPlus.Analyzer.Elements;
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
            Analyzer2Test(AnalyzerMode.Full,Path.Combine(AssetsFolder, "Debug.vhdp"));
        }
        
        [Fact]
        public void Analyzer2TestFullVhdl()
        {
            Analyzer2Test(AnalyzerMode.Full,Path.Combine(AssetsFolder, "Random_Number.vhd"));
        }

        [Fact]
        public void Analyzer2ReadVariables()
        {
            var result = RunAnalyzer(AnalyzerMode.Full, Path.Combine(AssetsFolder, "OV5647_Camera.vhdp"));

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
        public void AnalyzerGetSegmentFromOffsetTest()
        {
            var text = File.ReadAllText(Path.Combine(AssetsFolder, "OV5647_Camera.vhdp"));
            var result = Analyzer.Analyze("",text, AnalyzerMode.Full);

            var segment = AnalyzerHelper.GetSegmentFromOffset(result,  5217);
            
            Assert.NotNull(segment);
            Assert.Equal(SegmentType.If, segment?.SegmentType);
        }
        
        [Fact]
        public void AnalyzerParameterTests()
        {
            Assert.Empty(Analyzer.Analyze("","VARIABLE LED : INTEGER := (12) + (0 + (((1/10))/10)/10);", AnalyzerMode.Full).Diagnostics);
            Assert.Empty(Analyzer.Analyze("","VARIABLE LED : INTEGER := ((12) + (0 + (((1/10))/10)/10));", AnalyzerMode.Full).Diagnostics);
            Assert.Empty(Analyzer.Analyze("","VARIABLE LED : INTEGER := ((5) => (((5)/30)));", AnalyzerMode.Full).Diagnostics);
            Assert.Empty(Analyzer.Analyze("","VARIABLE LED : BOOLEAN := ((true) AND false);", AnalyzerMode.Full).Diagnostics);
        }

        [Fact]
        public void TypeCheckTest()
        {
            var result = RunAnalyzer(AnalyzerMode.Full,Path.Combine(AssetsFolder, "TypeCheck.vhdp"));
            foreach (var diag in result.Diagnostics)
            {
                _output.WriteLine("{0} {1}:{2}", diag.Message, diag.StartLine.ToString(), diag.StartCol.ToString());
            }
            Assert.Empty(result.Diagnostics);
            
            var result2 = RunAnalyzer(AnalyzerMode.Full,Path.Combine(AssetsFolder, "TypeCheckInvalid.vhdp"));
            foreach (var diag in result2.Diagnostics)
            {
                _output.WriteLine("{0} {1}:{2}", diag.Message, diag.StartLine.ToString(), diag.StartCol.ToString());
            }
            Assert.Single(result2.Diagnostics);
        }
    }
}
