namespace VHDPlus.Analyzer.Diagnostics;

public enum DiagnosticLevel
{
    Error,
    Warning,
    Hint
}

public interface IAnalyzerDiagnostic
{
    public DiagnosticLevel Level { get; }

    public int StartLine { get; }

    public int StartCol { get; }

    public int EndLine { get; }

    public int EndCol { get; }

    public string Message { get; }
}