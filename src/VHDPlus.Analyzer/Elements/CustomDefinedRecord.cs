namespace VHDPlus.Analyzer.Elements;

public class CustomDefinedRecord : DataType, IVariableOwner
{
    public CustomDefinedRecord(string name) : base(name)
    {
    }

    public override string Description => $"Record {Name}:\n{string.Join('\n', Variables)}";
    public Dictionary<string, DefinedVariable> Variables { get; } = new();
}