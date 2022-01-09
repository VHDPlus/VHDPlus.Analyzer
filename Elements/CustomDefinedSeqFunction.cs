namespace VHDPlus.Analyzer.Elements;

public class CustomDefinedSeqFunction : IParameterOwner
{
    public List<FunctionParameter> Parameters { get; } = new();
    public string Name { get; }
    public string? Description { get; }
    public Segment Owner { get; }
    
    public Dictionary<string, DefinedVariable> ExposingVariables { get; } = new();
    
    public CustomDefinedSeqFunction(Segment owner, string name, string? description = null)
    {
        Owner = owner;
        Name = name;
        Description = description;
    }
}