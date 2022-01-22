using VHDPlus.Analyzer.Checks;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer;

public static class AnalyzerHelper
{
    private static readonly List<(DataType, DataType)> ValidPairs = new()
    {
        (DataType.StdLogicVector, DataType.StdLogic),
        (DataType.StdLogicVector, DataType.Unsigned),
        (DataType.StdLogicVector, DataType.String),
        (DataType.String, DataType.StdLogicVector),
        (DataType.Unsigned, DataType.StdLogic),
        (DataType.Integer, DataType.Unsigned),
        (DataType.Integer, DataType.Signed),
        (DataType.Integer, DataType.StdLogicVector),
        (DataType.Integer, DataType.Positive),
        (DataType.Integer, DataType.Natural),
        (DataType.Integer, DataType.Real),
        (DataType.Real, DataType.Integer),
        (DataType.Natural, DataType.Integer),
        (DataType.Natural, DataType.Positive),
        (DataType.Positive, DataType.Integer),
        (DataType.Positive, DataType.Natural),
        (DataType.Time, DataType.Integer),
    };

    private static readonly List<(DataType, DataType)> ValidConcatPairs = new()
    {
        (DataType.StdLogicVector, DataType.StdLogic),
        (DataType.StdLogicVector, DataType.Unsigned),
        (DataType.StdLogicVector, DataType.Signed),
        (DataType.StdLogic, DataType.StdLogicVector),
        (DataType.StdLogic, DataType.Signed),
        (DataType.StdLogic, DataType.Unsigned),
        (DataType.Unsigned, DataType.StdLogicVector),
        (DataType.Unsigned, DataType.StdLogic),
        (DataType.Signed, DataType.StdLogicVector),
    };
    
    private static readonly List<(DataType, DataType)> ValidComparisonPairs = new()
    {
        (DataType.Unsigned, DataType.Natural),
        (DataType.Natural, DataType.Unsigned),
        (DataType.Unsigned, DataType.StdLogicVector),
        (DataType.StdLogicVector, DataType.Unsigned),
    };

    public static bool IsWordLetter(this char c)
    {
        return (c == '_' || char.IsLetter(c)) && c != ' ';
    }

    public static bool IsDigitOrWhiteSpace(this char c)
    {
        return char.IsWhiteSpace(c) || char.IsDigit(c);
    }

    public static char ToLower(this char c)
    {
        return char.ToLower(c);
    }

    public static bool AreTypesCompatible(DataType from, DataType to, string op)
    {
        return from.Name == to.Name || (from is CustomDefinedEnum && to is CustomDefinedEnum) || ValidPairs.Contains((from, to)) || op is "&" && ValidConcatPairs.Contains((@from, to)) || op is "<=" or ">=" or "<" or ">" && ValidComparisonPairs.Contains((from, to));
    }

    public static DefinedVariable? SearchVariable(Segment start, string? varName = null)
    {
        varName ??= start.NameOrValue;
        
        if (start.Context.AvailableExposingVariables.ContainsKey(varName.ToLower()))
            return start.Context.AvailableExposingVariables[varName.ToLower()];
        
        while (start.Parent != null)
        {
            if (start.Variables.ContainsKey(varName.ToLower())) return start.Variables[varName.ToLower()];
            start = start.Parent;
        }

        return null;
    }

    public static DefinedVariable? SearchVariableInRecord(Segment start)
    {
        if (start.ConcatOperator == "." && start.Parent is { })
        {
            if (TypeCheck.ConvertTypeParameter(start.Parent) is CustomDefinedRecord cdt &&
                cdt.Variables.ContainsKey(start.NameOrValue.ToLower())) return cdt.Variables[start.NameOrValue.ToLower()];
        }
        else if (start.Children.Any() && start.Children.First() is { ConcatOperator: "=>" } &&
                 SearchTopSegment(start, SegmentType.VariableDeclaration, SegmentType.DataVariable) is { } decl)
        {
            if (decl.DataType is CustomDefinedArray array && array.ArrayType is CustomDefinedRecord record &&
                record.Variables.ContainsKey(start.NameOrValue.ToLower())) return record.Variables[start.NameOrValue.ToLower()];
        }
        return null;
    }
    
    public static Segment SearchRecordParent(Segment start)
    {
        while (start.ConcatOperator == "." && start.Parent is { })
        {
            start = start.Parent;
        }
        return start;
    }

    public static CustomDefinedEnum? SearchEnum(AnalyzerContext context, string name)
    {
        foreach (var type in context.AvailableTypes)
            if (type.Value is CustomDefinedEnum e && e.States.Contains(name))
                return e;
        return null;
    }
    
    public static IEnumerable<CustomDefinedFunction>? SearchFunctions(Segment start, string name)
    {
        return start.Context.AvailableFunctions.ContainsKey(name.ToLower()) ? start.Context.AvailableFunctions[name.ToLower()] : null;
    }
    
    public static CustomDefinedFunction?  SearchFunction(Segment start, string name)
    {
        var functions = SearchFunctions(start, name.ToLower());

        if (functions == null) return null;
        
        //Find right overload
        if (start.Parameter.Any())
        {
            foreach (var f in functions)
            {
                bool valid = true;
                var funcPar = start.Parameter[0].Any() ? start.Parameter[0][0] : null;
                for (var i = 0; funcPar != null && i < f.Parameters.Count; i++)
                {
                    var from = TypeCheck.ChildOperatorCheck(funcPar);
                    if (!AreTypesCompatible(from.Item1, f.Parameters[i].DataType, ":="))
                    {
                        valid = false;
                        break;
                    }
                    funcPar = SearchNextOperatorChild(funcPar, ",");
                }
                if(valid) return f;
            }
        }

        return functions.First();
    }
    
    public static CustomDefinedSeqFunction? SearchSeqFunction(Segment start, string name)
    {
        return start.Context.AvailableSeqFunctions.ContainsKey(name.ToLower()) ? start.Context.AvailableSeqFunctions[name.ToLower()]
            : null;
    }

    public static Segment? SearchTopSegment(Segment start, params SegmentType[] type)
    {
        while (start.Parent != null)
        {
            if (type.Contains(start.SegmentType)) return start;
            start = start.Parent;
        }

        return null;
    }
    
    public static Segment SearchParameterOwner(Segment start)
    {
        while (start.Parent != null && InParameter(start))
        {
            start = start.Parent;
        }
        return start;
    }

    public static Segment? SearchOperatorChild(Segment start, params string[] operators)
    {
        var s = start;
        while (true)
        {
            if (operators.Contains(s.ConcatOperator)) return s;
            if (!s.Children.Any()) break;
            s = s.Children.First();
        }
        return null;
    }
    
    public static Segment? SearchNextOperatorChild(Segment start, params string[] operators)
    {
        var s = start;
        while (true)
        {
            if (!s.Children.Any()) break;
            s = s.Children.First();
            if (operators.Contains(s.ConcatOperator)) return s;
        }
        return null;
    }
    
    public static Segment? SearchTopOperator(Segment s, params string[] operators)
    {
        while (s.Parent != null)
        {
            if (operators.Contains(s.ConcatOperator)) return s;
            s = s.Parent;
        }
        return null;
    }
    
    public static Segment SearchConcatParent(Segment s, params SegmentType[] type)
    {
        while (s.Parent != null)
        {
            if (!s.ConcatSegment || type.Contains(s.SegmentType) || s.ConcatOperator is "when" or "and" or "or") return s;
            s = s.Parent;
        }
        return s;
    }
    
    public static bool InParameter(Segment start)
    {
        start = SearchConcatParent(start);
        return !start.Parent?.Children.Contains(start) ?? false;
    }

    public static Segment? GetSegmentFromOffset(AnalyzerContext context, int offset)
    {
        return context.InComment(offset) ? null : GetSegmentFromOffset(context.TopSegment.Children, offset);
    }

    /// <summary>
    ///     Gets Segment at specific offset
    ///     TODO: Binary Search?
    /// </summary>
    private static Segment? GetSegmentFromOffset(IEnumerable<Segment> segments, int offset)
    {
        foreach (var segment in segments)
            if ((segment.ConcatSegment && offset >= segment.ConcatOperatorIndex || offset >= segment.Offset) && offset <= segment.EndOffset)
            {
                var s = GetSegmentFromOffset(segment.Children.ToArray(), offset);
                foreach (var par in segment.Parameter)
                {
                    var p = GetSegmentFromOffset(par.ToArray(), offset);
                    if (p != null) return p;
                }
                if (s != null) return s;

                return segment;
            }

        return null;
    }

    public static IEnumerable<DefinedVariable> GetVariablesAtSegment(Segment segment)
    {
        
        foreach (var variable in segment.Context.AvailableExposingVariables)
        {
            yield return variable.Value;
        }
        
        while (segment.Parent != null)
        {
            foreach (var variable in segment.Variables) 
                if(!segment.Context.AvailableExposingVariables.ContainsKey(variable.Key)) yield return variable.Value;
            segment = segment.Parent;
        }
    }

    public static int GetBlockDepth(Segment s)
    {
        var d = 0;
        while (s.Parent != null)
        {
            if (s.ConcatOperator is null or "") d++;
            s = s.Parent;
        }

        return d;
    }

    public static int GetBlockDepth(AnalyzerContext context, int line)
    {
        var offset = context.GetOffset(line, 1);
        var s = GetSegmentFromOffset(context, offset);
        return s == null ? 0 : GetBlockDepth(s);
    }
}