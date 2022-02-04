namespace VHDPlus.Analyzer.Elements;

public class DataType
{
    public static readonly DataType Unknown = new("UNKNOWN");

    public static readonly DataType StdLogic = new("STD_LOGIC",
        "Type with states '0', '1', 'U', 'X', 'Z', 'W', 'L', 'H' and '-'");

    public static readonly DataType StdLogicVector =
        new("STD_LOGIC_VECTOR", "Array of STD_LOGIC\nSize e.g. (7 downto 0)");

    public static readonly DataType Integer = new("INTEGER",
        "Number with a maximum range of -2147483648 to 2147483647\nSize e.g. range 0 to 255");

    public static readonly DataType Signed = new("SIGNED",
        "Array of STD_LOGIC with support of computational operations\nSize e.g. (7 downto 0) with range of -128 to 127");

    public static readonly DataType Natural = new("NATURAL",
        "Number with a maximum range of 0 to 2147483647\nSize e.g. range 0 to 255");

    public static readonly DataType Unsigned = new("UNSIGNED",
        "Array of STD_LOGIC with support of computational operations\nSize e.g. (7 downto 0) with range of 0 to 255");

    public static readonly DataType Positive = new("POSITIVE",
        "Number with a maximum range of 1 to 2147483647\nSize e.g. range 1 to 256");

    public static readonly DataType Boolean = new("BOOLEAN", "Type with states false and true");
    public static readonly DataType Time = new("TIME", "Type for simulation to e.g. define time to wait");
    public static readonly DataType Others = new("OTHERS");

    public static readonly DataType String = new("STRING",
        "Type for simulation or constant signals\nUse the String_Type library or s\"...\" that creates a hex value for synthesizable code");

    public static readonly DataType Real = new("REAL",
        "Type for simulation or constant signals\nNeeds include of math_real library\nUse integer operations for synthesizable code");

    public static readonly DataType Bit = new("BIT", "Type with states '0' and '1'");
    public static readonly DataType BitVector = new("BIT_VECTOR", "Array of BIT\nSize e.g. (7 downto 0)");

    private readonly string? _description;

    protected DataType(string name, string? description = null)
    {
        Name = name;
        _description = description;
    }

    public string Name { get; }
    public virtual string Description => _description ?? "";

    public override string ToString()
    {
        return Name;
    }
}