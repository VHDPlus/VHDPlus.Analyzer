using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Predefined;

public class PredefinedTypes
{
    public static readonly DataType[] StdLogic1164 =
    {
    };

    public static readonly DataType[] NumericStd =
    {
        DataType.StdLogic, DataType.StdLogicVector, DataType.Integer, DataType.Signed, DataType.Natural,
        DataType.Unsigned, DataType.Positive, DataType.Boolean, DataType.Time, DataType.String, DataType.Bit,
        DataType.BitVector
    };

    public static readonly DataType[] MathReal =
    {
        DataType.Real
    };
}