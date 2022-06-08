namespace VHDPlus.Analyzer.Elements;

public class CustomBuiltinFunction : IParameterOwner
{
    private static readonly CustomBuiltinFunction Wait =
        new("Wait",
            "Delays two operations by a given number of CLK cycles\n\nFor 12MHz clocks you can use time units (e.g. 10ms, 100ns)")
        {
            Parameters = { new FunctionParameter("timespan", DataType.Integer) }
        };

    public static readonly Dictionary<string, CustomBuiltinFunction> DefaultBuiltinFunctions = new()
    {
        { "wait", Wait }
    };

    public CustomBuiltinFunction(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    public Dictionary<string, DefinedVariable> ExposingVariables { get; } = new();

    public List<FunctionParameter> Parameters { get; } = new();
    public string Name { get; }
    public string? Description { get; }
}