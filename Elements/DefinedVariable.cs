namespace VHDPlus.Analyzer.Elements;

public enum VariableType
{
    Unknown,
    Io,
    Signal,
    Variable,
    Constant,
    Iterator,
    ComponentMember,
    Generic,
    Attribute,
    RecordMember
}

public class DefinedVariable
{
    public DefinedVariable(Segment owner, string name, DataType dataType, VariableType varType, int offset)
    {
        Owner = owner;
        Name = name;
        DataType = dataType;
        VariableType = varType;
        Offset = offset;
    }

    public string Name { get; }
    public DataType DataType { get; set; }
    public VariableType VariableType { get; }
    public int Offset { get; }
    public Segment Owner { get; }

    public override string ToString()
    {
        return $"{Name} : {DataType}";
    }
}