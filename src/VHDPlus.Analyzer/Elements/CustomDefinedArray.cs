namespace VHDPlus.Analyzer.Elements;

public class CustomDefinedArray : DataType
{
    public CustomDefinedArray(string name) : base(name)
    {
    }

    public DataType ArrayType { get; set; } = Unknown;

    public override string Description => $"Array of {ArrayType}";
}