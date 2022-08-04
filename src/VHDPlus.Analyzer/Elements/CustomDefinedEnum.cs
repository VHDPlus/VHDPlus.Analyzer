namespace VHDPlus.Analyzer.Elements;

public class CustomDefinedEnum : DataType
{
    public CustomDefinedEnum(Segment owner, string name) : base(owner,name)
    {
    }

    public List<string> States { get; } = new();

    public override string Description => $"Enum with states: {string.Join(',', States)}";
}