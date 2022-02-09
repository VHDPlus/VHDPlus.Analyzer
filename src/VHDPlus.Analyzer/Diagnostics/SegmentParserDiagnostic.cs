using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Diagnostics;

public class SegmentParserDiagnostic : GenericAnalyzerDiagnostic
{
    public SegmentParserDiagnostic(AnalyzerContext context, string message, DiagnosticLevel level, Segment s) : base(
        context, message, level, s)
    { }
    
    public SegmentParserDiagnostic(AnalyzerContext context, string message, DiagnosticLevel level, int startIndex,
        int endIndex) : base(context, message, level, startIndex, endIndex)
    { }
    
    public SegmentParserDiagnostic(SegmentParserContext context, string message, DiagnosticLevel level) : base(context, message, level)
    { }
}