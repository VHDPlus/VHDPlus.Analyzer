using System.Net.Mime;
using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer;

public static class ParserHelper
{
    public static string[] VhdlOperators { get; } =
    {
        "and",
        "or",
        "nand",
        "nor",
        "xor",
        "xnor",
        "mod",
        "not",
        "abs",
        "rem",
        "sll",
        "srl",
        "sla",
        "sra",
        "rol",
        "ror"
    };

    public static string[] VhdlIos { get; } =
    {
        "in",
        "out",
        "inout",
        "buffer"
    };

    public static SegmentType GetSegmentType(string name)
    {
        Enum.TryParse(name, true, out SegmentType type);
        return type;
    }

    private static DataType GetNativeDataType(string val)
    {
        var tr = val.ToLower();
        switch (tr.Length)
        {
            case 3 when tr[0] == '\'' && tr[^1] == '\'':
                return DataType.StdLogic;
            case > 0 when tr.All(char.IsDigit):
                return DataType.Integer;
            case > 0 when tr.All(x => char.IsDigit(x) || x is '.' or 'e'):
                return DataType.Real;
            case > 1 when (tr[0] == '"' || tr[0] is 's' or 'x' && tr[1] == '"') && tr[^1] == '"':
                return DataType.StdLogicVector;
            case > 3 when tr is "false" or "true":
                return DataType.Boolean;
            case 6 when tr is "others":
                return DataType.Others;
            case > 1 when tr[^1] is 's':
                if (tr[^2].IsDigitOrWhiteSpace() && tr[..^1].All(AnalyzerHelper.IsDigitOrWhiteSpace) //1s
                    || tr.Length > 2 && tr[^2] is 'f' or 'm' or 'p' or 'n' or 'u' &&
                    tr[..^3].All(AnalyzerHelper.IsDigitOrWhiteSpace)) //ms, ps, ns
                    return DataType.Time;
                break;
            default:
                return DataType.Unknown;
        }

        return DataType.Unknown;
    }

    public static DataType GetDeclaredDataType(AnalyzerContext context, string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) return DataType.Unknown;
        var typeStr = VhdlIos.Contains(words[0].ToLower()) && words.Length > 1 ? words[1] : words[0];

        if (context.AvailableTypes.TryGetValue(typeStr.ToLower(), out var dataType))
        {
            return dataType;
        }
        return DataType.Unknown;
    }

    public static VariableType GetVariableType(string tr)
    {
        var words = tr.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (words.Length > 1)
            switch (words[0].ToLower())
            {
                case "signal":
                    return VariableType.Signal;
                case "constant":
                    return VariableType.Constant;
                case "variable":
                    return VariableType.Variable;
                case "attribute":
                    return VariableType.Attribute;
            }

        return VariableType.Unknown;
    }
}