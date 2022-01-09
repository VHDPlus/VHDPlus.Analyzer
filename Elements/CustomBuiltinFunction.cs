namespace VHDPlus.Analyzer.Elements;

public class CustomBuiltinFunction : IParameterOwner
{
    private static readonly CustomBuiltinFunction Wait = new("Wait", "Delays two operations by a given number of CLK cycles (use the wait calculator to calculate the cycles")
    {
        Parameters = {new FunctionParameter("timespan", DataType.Integer)}
    };
    
    public static readonly Dictionary<string, CustomBuiltinFunction> DefaultBuiltinFunctions = new()
    {
        {"wait", Wait},
    };
    
    public List<FunctionParameter> Parameters { get; } = new();
    public string Name { get; }
    public string? Description { get; }

    public Dictionary<string, DefinedVariable> ExposingVariables { get; } = new();
    
    public CustomBuiltinFunction(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }
}