namespace VHDPlus.Analyzer.Elements;

public enum SegmentType
{
    Unknown,
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
    VhdlFunctionReturn,
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

public class Segment
{
    public SegmentType SegmentType { get; set; }
    public AnalyzerContext Context { get; set; }
    public Segment? Parent { get; set; }
    public List<Segment> Children { get; } = new();
    public List<List<Segment>> Parameter { get; } = new();
    public int Offset { get; set; }
    public int EndOffset { get; set; } //End offset including children
    public string? Value { get; set; }
    public bool Concat { get; set; }
}