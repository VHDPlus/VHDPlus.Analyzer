namespace VHDPlus.Analyzer.Elements;

public class CustomDefinedOperator
{
    public CustomDefinedOperator(string op, string? description = null)
    {
        Operator = op;
        ReturnType = DataType.Unknown;
        Left = DataType.Unknown;
        Right = DataType.Unknown;
        Description = description;
    }
    public DataType Left { get; set; }
    public DataType Right { get; set; }
    public DataType ReturnType { get; set; }
    public string Operator { get; }
    public string? Description { get; }
    public Segment? Owner { get; set; }
}