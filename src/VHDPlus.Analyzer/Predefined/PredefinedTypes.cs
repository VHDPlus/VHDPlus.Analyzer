using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Predefined;

public class PredefinedTypes
{
    public static readonly Dictionary<string, DataType>  StdLogic1164 = new()
    {
    };
    
    public static readonly Dictionary<string, DataType> StdLogicArith = new()
    {
    };

    public static readonly Dictionary<string, DataType> NumericStd = new()
    {
        {"std_logic", DataType.StdLogic},
        {"std_logic_vector", DataType.StdLogicVector},
        {"integer", DataType.Integer},
        {"signed", DataType.Signed},
        {"natural", DataType.Natural},
        {"unsigned", DataType.Unsigned},
        {"positive", DataType.Positive},
        {"boolean", DataType.Boolean},
        {"time", DataType.Time},
        {"string", DataType.String},
        {"bit", DataType.Bit},
        {"bitvector", DataType.BitVector},
    };

    public static readonly Dictionary<string, DataType> MathReal = new()
    {
        {"real", DataType.Real},
    };
}