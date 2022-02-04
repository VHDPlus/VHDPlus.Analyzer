using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Diagnostics;

public class GenericAnalyzerDiagnostic : IAnalyzerDiagnostic
{
    private GenericAnalyzerDiagnostic(AnalyzerContext context, DiagnosticLevel level, int startIndex, int endIndex)
    {
        Level = level;
        StartLine = context.GetLine(startIndex);
        StartCol = context.GetCol(startIndex);
        EndLine = context.GetLine(endIndex);
        EndCol = context.GetCol(endIndex);
        Message = "";
    }

    public GenericAnalyzerDiagnostic(AnalyzerContext context, string message, DiagnosticLevel level, int startIndex,
        int endIndex) : this(context, level, startIndex, endIndex)
    {
        Message = message;
    }

    public GenericAnalyzerDiagnostic(AnalyzerContext context, string message, DiagnosticLevel level, Segment s) : this(
        context, message, level, s.Offset, s.Offset + s.NameOrValue.Length)
    {
    }

    public GenericAnalyzerDiagnostic(SegmentParserContext context, string message, DiagnosticLevel level) : this(
        context.AnalyzerContext, message, level, context.CurrentInnerIndex, context.LastInnerIndex)
    {
    }

    public DiagnosticLevel Level { get; }

    public string Message { get; }

    public int StartLine { get; }

    public int StartCol { get; }

    public int EndLine { get; }

    public int EndCol { get; }
}