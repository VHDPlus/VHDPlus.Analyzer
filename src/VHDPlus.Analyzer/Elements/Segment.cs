namespace VHDPlus.Analyzer.Elements;

public enum SegmentType
{
    Unknown,
    GlobalSegment,
    EmptyName,
    Component,
    Package,
    NewComponent,
    Vhdl,
    Main,
    Process,
    Thread,
    Function,
    FunctionContent,
    SeqFunction,
    SeqFor,
    NewFunction,
    Return,
    Null,
    TypeUsage,
    Type,
    SubType,
    Connections,
    ConnectionsMember,
    Step,
    Generate,
    While,
    CustomBuiltinFunction,
    If,
    When,
    With,
    Case,
    Generic,
    Include,
    AttributeDeclaration,
    Else,
    Elsif,
    For,
    VariableDeclaration,
    DataVariable,
    NativeDataValue,
    ComponentMember,
    VhdlFunction,
    Record,
    Array,
    Range,
    IncludePackage,
    VhdlEnd,
    EnumDeclaration,
    Enum,
    ParFor,
    Class,
    SeqWhile,
    Exit,
    ParWhile,
    None,
    Open,
    VhdlAttribute,
    Attribute,
    Begin,
    Then,
    Port
}

public class Segment : IVariableOwner
{
    public Segment(AnalyzerContext context, Segment? parent, string nameOrValue, SegmentType segmentType,
        DataType dataType, int offset,
        string? concatOperator = null, int concatOperatorIndex = 0)
    {
        Context = context;
        Parent = parent;
        NameOrValue = nameOrValue;
        SegmentType = segmentType;
        DataType = dataType;
        Offset = offset;
        ConcatOperator = concatOperator;
        ConcatOperatorIndex = concatOperatorIndex;
    }

    public AnalyzerContext Context { get; }
    public SegmentType SegmentType { get; set; }
    public DataType DataType { get; set; }
    public int Offset { get; }
    public int EndOffset { get; set; } //End offset including children
    public Segment? Parent { get; }
    public List<Segment> Children { get; } = new();
    public List<List<Segment>> Parameter { get; } = new();
    public string NameOrValue { get; }
    public bool ConcatSegment => ConcatOperator != null;
    public string? ConcatOperator { get; set; }
    public int ConcatOperatorIndex { get; }
    public bool SymSegment { get; set; } //Segment ending with ;
    public string LastName => NameOrValue.Split(' ').Last();
    public Dictionary<string, DefinedVariable> Variables { get; } = new();

    public override string ToString()
    {
        return NameOrValue;
    }
}