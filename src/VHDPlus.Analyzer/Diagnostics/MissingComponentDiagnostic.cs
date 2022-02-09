using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Diagnostics;

public class MissingComponentDiagnostic : GenericAnalyzerDiagnostic
{
    public MissingComponentDiagnostic(AnalyzerContext context, string message, DiagnosticLevel level, Segment s) : base(
        context, message, level, s)
    { }
}