using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer;

[Flags]
public enum AnalyzerMode
{
    Indexing,
    Resolve,
    Check,
}

public static class Analyzer
{
    public static AnalyzerContext Analyze(string path, string content, AnalyzerMode mode, ProjectContext? pC = null)
    {
        var context = SegmentParser.Parse(path, content);

        return Analyze(context, mode, pC);
    }

    public static AnalyzerContext Analyze(AnalyzerContext context, AnalyzerMode mode, ProjectContext? pC)
    {
        if (pC != null) context.AddProjectContext(pC);

        if (mode.HasFlag(AnalyzerMode.Resolve) || mode.HasFlag(AnalyzerMode.Check))
        {
            //Filter out all diagnostics that are not from segment parsing 
            context.Diagnostics.RemoveAll(x => x is not (SegmentParserDiagnostic or ResolveDiagnostic));
        }

        if (mode.HasFlag(AnalyzerMode.Resolve))
        {
            context.Diagnostics.RemoveAll(x => x is ResolveDiagnostic);

            context.ResolveIncludes();
            //ResolveMissingTypes(context, context.AvailableTypes);
            //ResolveMissingSeqFunctions(context);
            //ResolveMissingComponents(context);
            //ResolveMissingSegments(context);
        }
        if (mode.HasFlag(AnalyzerMode.Check))
        {
            //ErrorCheck(context);
        }
        return context;
    }
    
}