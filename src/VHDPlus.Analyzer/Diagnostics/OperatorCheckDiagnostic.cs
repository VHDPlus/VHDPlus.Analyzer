using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Diagnostics;

public class OperatorCheckDiagnostic : GenericAnalyzerDiagnostic
{
    public OperatorCheckDiagnostic(AnalyzerContext context, string message, DiagnosticLevel level, Segment s) : base(
        context, message, level, s)
    { }
    
    public OperatorCheckDiagnostic(AnalyzerContext context, string message, DiagnosticLevel level, int startIndex,
        int endIndex) : base(context, message, level, startIndex, endIndex)
    { }
}