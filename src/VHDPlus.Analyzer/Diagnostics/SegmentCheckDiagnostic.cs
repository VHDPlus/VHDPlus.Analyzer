using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Diagnostics;

public class SegmentCheckDiagnostic : GenericAnalyzerDiagnostic
{
    public SegmentCheckDiagnostic(AnalyzerContext context, string message, DiagnosticLevel level, Segment s) : base(
        context, message, level, s)
    { }
}