using VHDPlus.Analyzer.Info;

namespace VHDPlus.Analyzer.Elements;

public interface IParameterOwner
{
    public string Name { get; }
    
    public string? Description { get; }
    public List<FunctionParameter> Parameters { get; }
}