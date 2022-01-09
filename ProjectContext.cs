using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer;

public class ProjectContext
{
    public List<AnalyzerContext> Files { get; } = new();
}