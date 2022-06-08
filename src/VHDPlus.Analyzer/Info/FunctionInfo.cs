using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Info;

public static class FunctionInfo
{
    public static string GetInfoMarkdown(CustomDefinedFunction function)
    {
        var str = $"```vhdp\n{function.Name}(";
        foreach (var par in function.Parameters)
        {
            str += $"{par.Name} : {par.DataType}";
            if (par != function.Parameters.Last()) str += ", ";
        }

        str += $")\nreturn : {function.ReturnType}\n```";
        return str;
    }

    public static string GetInfoMarkdown(CustomDefinedSeqFunction function)
    {
        var str = $"```vhdp\n{function.Name}(";
        foreach (var par in function.Parameters)
        {
            str += $"{par.Name} : {par.DataType}";
            if (par != function.Parameters.Last()) str += ", ";
        }

        str += ")\n```";
        return str;
    }
    
    public static string GetInfoMarkdown(CustomBuiltinFunction function)
    {
        var str = $"```vhdp\n{function.Name}(";
        foreach (var par in function.Parameters)
        {
            str += $"{par.Name} : {par.DataType}";
            if (par != function.Parameters.Last()) str += ", ";
        }

        str += $")\n```\n{function.Description}";
        return str;
    }

    public static string GetInsert(CustomDefinedFunction function)
    {
        return $"{function.Name}($0)";
    }
}