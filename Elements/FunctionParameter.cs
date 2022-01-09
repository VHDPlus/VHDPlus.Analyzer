namespace VHDPlus.Analyzer.Elements;

public class FunctionParameter
{
    public string Name { get; }
    public DataType DataType { get; set; }

    public FunctionParameter(string name, DataType dataType)
    {
        Name = name;
        DataType = dataType;
    }
}