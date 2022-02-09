using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Checks;

public static class TypeCheck
{
    
    public static void CheckFunctionParameter(AnalyzerContext context, Segment function)
    {
        CustomBuiltinFunction.DefaultBuiltinFunctions.TryGetValue(function.LastName.ToLower(), out var builtin);
        IParameterOwner? func = builtin;
        func ??= AnalyzerHelper.SearchFunction(function, function.LastName);
        func ??= AnalyzerHelper.SearchSeqFunction(function, function.LastName);
        if (func == null) return;
        var funcPar = function.Parameter.Any() ? function.Parameter[0].Any() ? function.Parameter[0][0] : null : null;
        var i = 0;
        for (; funcPar != null; i++)
        {
            if (func.Parameters.Count > i)
            {
                var (fD, segment) = ChildOperatorCheck(funcPar);
                if (fD != DataType.Unknown && !AnalyzerHelper.AreTypesCompatible(fD, func.Parameters[i].DataType, "="))
                    context.Diagnostics.Add(new TypeCheckDiagnostic(context,
                        $"Invalid Parameter of type {fD}. Expected {func.Parameters[i].DataType}",
                        DiagnosticLevel.Error, funcPar));
            }
            else
            {
                context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context, "More parameters provided than expected",
                    DiagnosticLevel.Warning, funcPar));
                break;
            }

            funcPar = AnalyzerHelper.SearchNextOperatorChild(funcPar, ",");
        }

        if (func.Parameters.Count > i + 1)
            context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context, "Less parameters provided than expected",
                DiagnosticLevel.Warning, function));
    }

    public static void CheckTypePair(Segment parent, Segment child, AnalyzerContext context)
    {
        var currentOperator = child.ConcatOperator;
        //Check types
        if (currentOperator is null or "when" or "is" or "else" or "," or "and" or "." or ":" or "or" or "of" or "not") return;
        
        var (fD, segment) = ChildOperatorCheck(child);
        child = segment;
        var tD = ConvertTypeParameter(parent);

        if (fD != DataType.Others && fD != DataType.Unknown && parent.DataType != DataType.Unknown &&
            parent.DataType != DataType.Others)
        {
            if (!AnalyzerHelper.InParameter(child) &&
                parent is { Parent: { }, ConcatOperator: "=" or "<" or ">" or ">=" or "<=" or "/=" } &&
                parent.DataType == DataType.Integer
                && (parent.Parent.DataType ==
                    DataType.Integer ||
                    parent.Parent.DataType == DataType.Unknown)
                && child.ConcatOperator is not ("-" or "+"
                    or "*" or "/"))
                tD = DataType.Boolean;

            var operation = currentOperator switch
            {
                "&" => "concat",
                "or" => "compare",
                "and" => "compare",
                "=" => "compare",
                "/=" => "compare",
                ">=" => "compare",
                ">" => "compare",
                "<" => "compare",
                _ => "assign"
            };

            if (!AnalyzerHelper.AreTypesCompatible(fD, tD, currentOperator))
                context.Diagnostics.Add(new TypeCheckDiagnostic(context, $"Cannot {operation} {fD} to {tD}",
                    DiagnosticLevel.Warning, child));
        }
    }

    private static Segment GetRecordChild(Segment s)
    {
        while (s.Children.Any() && s.Children.First().ConcatOperator is ".")
        {
            if (!s.Children.Any()) break;
            s = s.Children.First();
        }

        return s;
    }

    public static (DataType, Segment) ChildOperatorCheck(Segment s)
    {
        s = GetRecordChild(s);
        var sD = ConvertTypeParameter(s);
        if (!s.Children.Any()) return (sD, s);
        var child = s.Children.First();

        var cD = ConvertTypeParameter(child);
        if (child.ConcatOperator is "-") cD = DataType.Integer;

        switch (child.ConcatOperator)
        {
            case "<" or ">" or "=" or "/="
                when s.ConcatOperator is not ("/" or "+" or "-" or "*" or "**" or "mod" or "&"):
                return (DataType.Boolean, s);
            case "&" when (cD == DataType.StdLogicVector || cD == DataType.StdLogic) &&
                          (sD == DataType.StdLogicVector || sD == DataType.StdLogic):
                return (DataType.StdLogicVector, s);
            case "&" when (cD == DataType.Unsigned || cD == DataType.StdLogic) &&
                          (sD == DataType.Unsigned || sD == DataType.StdLogic):
                return (DataType.Unsigned, s);
            case "'" when sD == DataType.StdLogicVector || sD == DataType.Signed || sD == DataType.Unsigned:
                return (DataType.Integer, s);
            default:
                return (sD, s);
        }
    }

    public static DataType ConvertTypeParameter(Segment s)
    {
        var data = s.DataType;
        for (var i = 0; i < s.Parameter.Count; i++) data = ConvertParameter(s, data, i);
        return data;
    }

    private static DataType ConvertParameter(Segment s, DataType type, int parameterIndex)
    {
        var par = s.Parameter[parameterIndex];
        if (s.SegmentType is SegmentType.VhdlFunction or SegmentType.Function)
        {
            if (AnalyzerHelper.SearchFunction(s, s.NameOrValue) is { } func) return func.ReturnType;
            return type;
        }

        if (par.Any() && AnalyzerHelper.SearchOperatorChild(par.First(), "downto") != null) return type;
        if (type == DataType.StdLogicVector || type == DataType.Signed) return DataType.StdLogic;
        if (type == DataType.Unsigned) return DataType.StdLogic;
        if (type is CustomDefinedArray array)
            return array.ArrayType;
        return type;
    }
}