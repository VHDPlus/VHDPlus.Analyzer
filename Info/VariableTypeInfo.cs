using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Info;

public static class VariableTypeInfo
{
    private static readonly Dictionary<VariableType, string> Info = new()
    {
        { VariableType.Constant, "Constant Declaration" },
        { VariableType.Variable, "Variable Declaration" },
        { VariableType.Signal, "Signal Declaration" }
    };

    public static string GetInfo(VariableType type)
    {
        Info.TryGetValue(type, out var description);
        return description ?? "";
    }
}