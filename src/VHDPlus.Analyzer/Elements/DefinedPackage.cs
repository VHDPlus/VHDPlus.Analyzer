namespace VHDPlus.Analyzer.Elements;

public class DefinedPackage
{
    public static readonly List<DataType> DataTypes = new();
    public static readonly Dictionary<string, IEnumerable<CustomDefinedFunction>> Functions = new();
}