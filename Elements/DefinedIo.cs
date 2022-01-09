namespace VHDPlus.Analyzer.Elements;

public enum IoType {In, Out, InOut, Buffer}

public class DefinedIo : DefinedVariable
{
    public IoType IoType { get; }
    
    public DefinedIo(Segment owner, string name, DataType dataType, VariableType varType, IoType ioType, int offset) : 
        base(owner, name, dataType, varType, offset)
    {
        IoType = ioType;
    }
    
    public override string ToString()
    {
        return $"{Name} : {IoType} {DataType}";
    }
}