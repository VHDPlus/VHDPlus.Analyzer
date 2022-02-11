namespace VHDPlus.Analyzer.Diagnostics;

public class ResolveDiagnostic : GenericAnalyzerDiagnostic
{
    public ResolveDiagnostic(AnalyzerContext context, string message, DiagnosticLevel level, int startIndex,
        int endIndex) : base(context, message, level, startIndex, endIndex)
    {
    }
}