namespace VHDPlus.Analyzer.Elements;

public class FunctionParameter
{
    public FunctionParameter(string name, DataType dataType)
    {
        Name = name;
        DataType = dataType;
    }

    public string Name { get; }
    public DataType DataType { get; set; }
}