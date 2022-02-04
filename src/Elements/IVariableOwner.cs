namespace VHDPlus.Analyzer.Elements;

public interface IVariableOwner
{
    public Dictionary<string, DefinedVariable> Variables { get; }
}