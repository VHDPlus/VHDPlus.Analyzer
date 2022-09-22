using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Predefined;

public static class PredefinedTypes
{
    public static readonly Dictionary<string, DataType> Standard = new()
    {
        {"bit", DataType.Bit},
        {"bit_vector", DataType.BitVector},
        {"boolean", DataType.Boolean},
        {"string", DataType.String},
        {"time", DataType.Time},
        {"real", DataType.Real},
        {"integer", DataType.Integer},
        {"natural", DataType.Natural},
        {"positive", DataType.Positive},
    };

    public static readonly Dictionary<string, DataType>  StdLogic1164 = new()
    {
        {"std_logic", DataType.StdLogic},
        {"std_ulogic", DataType.StdULogic},
        {"std_logic_vector", DataType.StdLogicVector},
        {"std_ulogic_vector", DataType.StdULogicVector},
    };
    
    public static readonly Dictionary<string, DataType> StdLogicArith = new()
    {
        {"signed", DataType.Signed},
        {"unsigned", DataType.Unsigned},
    };

    public static readonly Dictionary<string, DataType> NumericStd = new()
    {
        {"signed", DataType.Signed},
        {"unsigned", DataType.Unsigned},
    };

    public static readonly Dictionary<string, DataType> MathReal = new()
    {
    };
}