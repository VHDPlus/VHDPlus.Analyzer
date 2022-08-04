namespace VHDPlus.Analyzer.Elements;

public class CustomDefinedArray : DataType
{
    public CustomDefinedArray(Segment owner, string name) : base(owner, name)
    {
    }

    public DataType ArrayType { get; set; } = Unknown;

    public override string Description => $"Array of {ArrayType}";
}