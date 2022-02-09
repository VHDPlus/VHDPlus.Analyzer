using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Diagnostics;

public class TypeCheckDiagnostic : GenericAnalyzerDiagnostic
{
    public TypeCheckDiagnostic(AnalyzerContext context, string message, DiagnosticLevel level, Segment s) : base(
        context, message, level, s)
    { }
}