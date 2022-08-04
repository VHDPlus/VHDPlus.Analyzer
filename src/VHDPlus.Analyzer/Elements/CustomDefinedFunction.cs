namespace VHDPlus.Analyzer.Elements;

public class CustomDefinedFunction : IParameterOwner
{
    public CustomDefinedFunction(string name, string? description = null)
    {
        Name = name;
        ReturnType = DataType.Unknown;
        Description = description;
    }

    public DataType ReturnType { get; set; }
    public List<FunctionParameter> Parameters { get; } = new();
    public string Name { get; }
    public string? Description { get; }
}