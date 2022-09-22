using System.Net.Mime;
using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer;

public static class ParserHelper
{
    /// <summary>
    ///     After ': vector'length
    /// </summary>
    public static readonly string[] VhdlAttributes =
    {
        "length",
        "reverse_range",
        "range"
    };

    public static readonly char[] ValidStdLogic =
    {
        'U', 'X', '0', '1', 'Z', 'W', 'L', 'H', '-'
    };

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
        var words = name.Split(' ');
        if (words[0].All(char.IsDigit)) return SegmentType.NativeDataValue;
        Enum.TryParse(words[0], true, out SegmentType type);
        if (type == SegmentType.Unknown && words[0].Equals("impure")) type = SegmentType.Function;
        return type;
    }

    public static (SegmentType, DataType) CheckSegment(ref string name, SegmentParserContext context,
        bool checkVariable)
    {
        var words = name.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (name == "")
        {
            if (context.CurrentConcatOperator is "is" &&
                context.CurrentSegment.SegmentType is SegmentType.Type)
            {
                var enumName = context.CurrentSegment.NameOrValue.Split(' ',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Last();
                
                context.AnalyzerContext.AddLocalType(enumName.ToLower(), new CustomDefinedEnum(context.CurrentSegment, enumName), context.CurrentSegment);
               
                return (SegmentType.EnumDeclaration, DataType.Unknown);
            }

            if (context.CurrentConcatOperator is "'" &&
                VhdlAttributes.Contains(context.CurrentSegment.NameOrValue.ToLower()))
                context.AnalyzerContext.UnresolvedSegments.Remove(context.CurrentSegment);

            return (SegmentType.EmptyName, DataType.Unknown);
        }

        if (context.CurrentConcatOperator is "end") return (SegmentType.VhdlEnd, DataType.Unknown);

        var type = GetSegmentType(name);

        switch (type)
        {
            case SegmentType.Include:
                context.AnalyzerContext.IncludeExists = true;
                return (SegmentType.Include, DataType.Unknown);
            case SegmentType.Component when words.Length == 3 && words[2].ToLower() is "port":
                context.CurrentInner.Remove(context.CurrentInner.Length - words[2].Length - 1,
                    words[2].Length);
                context.LastInnerIndex = context.CurrentInnerIndex + words[1].Length + words[2].Length;
                context.PushSegment();
                context.CurrentInnerIndex = context.LastInnerIndex + 1;
                context.LastInnerIndex = context.CurrentInnerIndex + words[2].Length;
                name = "port";
                return (SegmentType.Port, DataType.Unknown);
            case SegmentType.Port:
                context.CurrentConcatOperator = null;
                return (type, DataType.Unknown);
            case SegmentType.Record:
            case SegmentType.Array:
                if (context.CurrentConcatOperator is "is" && context.CurrentSegment is
                        { SegmentType: SegmentType.Type })
                {
                    var ownerWords = context.CurrentSegment.NameOrValue.Split(' ');
                    if (ownerWords.Length == 2)
                    {
                        context.AnalyzerContext.AddLocalType(ownerWords[1].ToLower(),
                            type is SegmentType.Record ? new CustomDefinedRecord(context.CurrentSegment, ownerWords[1]) : new CustomDefinedArray(context.CurrentSegment, ownerWords[1]), context.CurrentSegment);
                    }
                }

                return (type, DataType.Unknown);
            case SegmentType.NewComponent:
            {
                return (SegmentType.NewComponent, DataType.Unknown);
            }
            case SegmentType.NewFunction:
            {
                return (SegmentType.NewFunction, DataType.Unknown);
            }
            case SegmentType.Function:
            {
                if (!context.AnalyzerContext.AvailableFunctions.ContainsKey(words.Last().ToLower()))
                    context.AnalyzerContext.AddLocalFunction(words.Last().ToLower(),
                        new CustomDefinedFunction(words.Last()));
                return (SegmentType.Function, DataType.Unknown);
            }
            case SegmentType.Return when words.Length == 2 && context.CurrentSegment is
                { SegmentType: SegmentType.Function }:
                var func = AnalyzerHelper.SearchFunction(context.CurrentSegment, context.CurrentSegment.LastName);
                if (func != null)
                {
                    var dataType = GetDeclaredDataType(context.AnalyzerContext, words[1]);
                    func.ReturnType = dataType;
                    return (SegmentType.Return, dataType);
                }

                return (SegmentType.Return, DataType.Unknown);
            case SegmentType.SeqFunction:
                return (SegmentType.SeqFunction, DataType.Unknown);
            case SegmentType.For:
                if (words.Length > 1)
                    if (!context.CurrentSegment.Variables.ContainsKey(words[1].ToLower()))
                        context.CurrentSegment.Variables.Add(words[1].ToLower(),
                            new DefinedVariable(context.CurrentSegment, words[1], DataType.Integer,
                                VariableType.Iterator,
                                context.CurrentIndex - name.Length));
                return (type, DataType.Unknown);
            case SegmentType.Unknown:
            case SegmentType.NativeDataValue:
                if (type is not SegmentType.NativeDataValue && (context.CurrentConcatOperator is ":" ||
                                                                VhdlIos.Contains(context.CurrentConcatOperator) &&
                                                                context.CurrentSegment is not
                                                                    { Parent.SegmentType: SegmentType.For }))
                {
                    if (context.VhdlMode && (context.CurrentSegment is { ConcatOperator: "of" } ||
                                             name.EndsWith("port map", StringComparison.OrdinalIgnoreCase)))
                        return (SegmentType.Attribute, DataType.Unknown);
                    var dataType = ParseDeclaration(context, name);
                    return (SegmentType.TypeUsage, dataType);
                }
                else if (context.CurrentSegment.SegmentType is SegmentType.SubType)
                {
                    var dataType = GetDeclaredDataType(context.AnalyzerContext, name);

                    var decName = context.CurrentSegment.LastName.ToLower();
                    if(decName.Length > 0)
                        context.AnalyzerContext.AddLocalType(context.CurrentSegment.LastName.ToLower(), dataType, context.CurrentSegment);
                    return (SegmentType.TypeUsage, dataType);
                }
                else if (context.CurrentConcatOperator is "of")
                {
                    if (context.VhdlMode && context.CurrentSegment.SegmentType is SegmentType.Attribute)
                        return (SegmentType.DataVariable, DataType.Unknown);
                    var dataType = ParseArrayDeclaration(context, name);
                    return (SegmentType.TypeUsage, dataType);
                }
                else if (context.CurrentSegment.SegmentType is SegmentType.For or SegmentType.ParFor &&
                         context.CurrentParsePosition == ParsePosition.Parameter)
                {
                    var varName = words[0];
                    if (varName.ToLower() is not ("variable" or "signal") && context.CurrentSegment.Parameter.Count == 0 &&
                        !context.CurrentSegment.Variables.ContainsKey(varName.ToLower()))
                    {
                        context.CurrentSegment.Variables.Add(varName.ToLower(),
                            new DefinedVariable(context.CurrentSegment, varName, DataType.Integer,
                                VariableType.Iterator,
                                context.CurrentIndex - name.Length));
                        return (type, DataType.Unknown);
                    }

                    return (SegmentType.Unknown, DataType.Unknown);
                }
                else if (context.CurrentSegment.SegmentType is SegmentType.Connections or SegmentType.ConnectionsMember)
                {
                    if (context.CurrentChar is ',' or '}')
                    {
                        if (context.CurrentSegment.SegmentType is SegmentType.ConnectionsMember &&
                            context.CurrentConcatOperator is "=>")
                        {
                            var connectionsName = context.CurrentSegment.NameOrValue.ToLower().Split('[')[0];
                            if (!context.AnalyzerContext.Connections.ContainsKey(connectionsName))
                                context.AnalyzerContext.Connections.Add(connectionsName,
                                    new ConnectionMember(connectionsName, name));
                        }
                        else
                        {
                            var connectionsName = name.ToLower().Split('[')[0];
                            if (!context.AnalyzerContext.Connections.ContainsKey(connectionsName))
                                context.AnalyzerContext.Connections.Add(connectionsName,
                                    new ConnectionMember(connectionsName));
                        }
                    }

                    return (SegmentType.ConnectionsMember, DataType.Unknown);
                }
                else if (checkVariable && GetNativeDataType(name) is { } dataType && dataType != DataType.Unknown)
                {
                    return (SegmentType.NativeDataValue, dataType);
                }
                else if (context.CurrentConcatOperator is "." &&
                         AnalyzerHelper.SearchVariable(context.CurrentSegment, context.CurrentSegment.NameOrValue) is
                             { } defined)
                {
                    if (defined.DataType is CustomDefinedRecord cdt)
                        if (cdt.Variables.ContainsKey(name.ToLower()))
                            return (SegmentType.DataVariable, cdt.Variables[name.ToLower()].DataType);
                    return (SegmentType.Unknown, DataType.Unknown);
                }
                else if (context.CurrentSegment.SegmentType is SegmentType.NewComponent ||
                         context.CurrentConcatOperator is "," &&
                         AnalyzerHelper.SearchConcatParent(context.CurrentSegment).Parent is
                             { SegmentType: SegmentType.NewComponent })
                {
                    return (SegmentType.ComponentMember, DataType.Unknown);
                }
                else if (context.CurrentSegment.SegmentType is SegmentType.EnumDeclaration or SegmentType.Enum &&
                         context.CurrentParsePosition is ParsePosition.Parameter)
                {
                    var topEnum = AnalyzerHelper.SearchTopSegment(context.CurrentSegment, SegmentType.EnumDeclaration)
                        ?.Parent;
                    if (topEnum is { SegmentType: SegmentType.Type or SegmentType.SubType })
                    {
                        var enumName = topEnum.NameOrValue.Split(' ').Last().ToLower();
                        if (context.AnalyzerContext.AvailableTypes.ContainsKey(enumName) &&
                            context.AnalyzerContext.AvailableTypes[enumName] is CustomDefinedEnum customEnum)
                            customEnum.States.Add(name);

                        return (SegmentType.Enum, DataType.Unknown);
                    }

                    return (SegmentType.Unknown, DataType.Unknown);
                }
                else if (context.CurrentConcatOperator is not "'" &&
                         AnalyzerHelper.SearchVariable(context.CurrentSegment, name) is
                             { } variable)
                {
                    return (SegmentType.DataVariable, variable.DataType);
                }
                else if (AnalyzerHelper.SearchEnum(context.AnalyzerContext, name) is { } definedEnum)
                {
                    return (SegmentType.DataVariable, definedEnum);
                }
                else if (context.CurrentChar is '(' &&
                         CustomBuiltinFunction.DefaultBuiltinFunctions.ContainsKey(name.ToLower()))
                {
                    return (SegmentType.CustomBuiltinFunction, DataType.Unknown);
                }
                else if (words.Length == 3 && words[1].ToLower() is "range")
                {
                    return (SegmentType.Range, DataType.Unknown);
                }
                else if (context.CurrentSegment.SegmentType is SegmentType.Include or SegmentType.IncludePackage)
                {
                    if (!context.AnalyzerContext.Includes.Any() || context.CurrentConcatOperator is not ".")
                        context.AnalyzerContext.Includes.Add("");
                    context.AnalyzerContext.Includes[^1] += (context.CurrentConcatOperator is "." ? "." : "") + name;
                    return (SegmentType.IncludePackage, DataType.Unknown);
                }
                else if (context.CurrentConcatOperator is "'" &&
                         VhdlAttributes.Contains(name.ToLower()))
                {
                    return (SegmentType.VhdlAttribute, DataType.Unknown);
                }
                else if (context.CurrentParsePosition is ParsePosition.Parameter &&
                         context.CurrentChar is '=' && context.NextChar is '>')
                {
                    return (SegmentType.VariableDeclaration, DataType.Unknown);
                }
                else if (context.VhdlMode && words.Last().ToLower() is "generate")
                {
                    return (SegmentType.Generate, DataType.Unknown);
                }
                else if (context.VhdlMode && words.Last().ToLower() is "then")
                {
                    return (SegmentType.Then, DataType.Unknown);
                }
                else if (words.First() is "wait") //For GHDP
                {
                    return (SegmentType.VhdlFunction, DataType.Unknown);
                }
                else if (context.CurrentSegment.SegmentType is SegmentType.SubType && context.CurrentConcatOperator is "is")
                {
                    return (SegmentType.TypeUsage, DataType.Unknown);
                }
                else
                {
                    return (SegmentType.Unknown, DataType.Unknown);
                }
            default:
                return (type, DataType.Unknown);
        }
    }

    public static Segment? SearchParentNoConcatSegment(Segment start)
    {
        while (start.Parent != null)
        {
            if (!start.ConcatSegment) return start;
            start = start.Parent;
        }

        return null;
    }

    public static Segment? GetLastOperatorSegment(Segment start, params string[] ops)
    {
        while (start.Parent != null)
        {
            if (!start.ConcatSegment) break;
            if (ops.Contains(start.ConcatOperator)) return start;
            start = start.Parent;
        }

        return null;
    }

    private static DataType ParseArrayDeclaration(SegmentParserContext context, string name)
    {
        var dataType = GetDeclaredDataType(context.AnalyzerContext, name);
        if (AnalyzerHelper.SearchTopSegment(context.CurrentSegment, SegmentType.Array) is
            { Parent: { SegmentType: SegmentType.Type } } array)
        {
            var typeName = array.Parent?.NameOrValue.Split(' ').Last().ToLower() ?? string.Empty;
            if (context.AnalyzerContext.AvailableTypes.ContainsKey(typeName) &&
                context.AnalyzerContext.AvailableTypes[typeName] is CustomDefinedArray cArray)
                cArray.ArrayType = dataType;
        }

        return dataType;
    }

    //std_logic, io std_logic
    private static DataType ParseDeclaration(SegmentParserContext context, string name)
    {
        //Get Names (in case concat with ,)
        var names = new List<Segment>();

        var variableType = VhdlIos.Contains(context.CurrentConcatOperator) ? VariableType.Io : VariableType.Unknown;

        Enum.TryParse(context.CurrentConcatOperator, true, out IoType ioType);

        var segment = context.CurrentSegment;
        if (segment.SegmentType is not (SegmentType.Unknown or SegmentType.VariableDeclaration or SegmentType.EmptyName
            or SegmentType.DataVariable or SegmentType.Enum or SegmentType.Attribute)) return DataType.Unknown;
        do
        {
            if (segment.ConcatOperator is not ":")
            {
                names.Add(segment);
                if (segment.ConcatOperator is not ",")
                {
                    if (variableType == VariableType.Unknown) variableType = GetVariableType(segment.NameOrValue);
                    segment.SegmentType = SegmentType.VariableDeclaration;
                    break;
                }
            }

            segment = segment.Parent;
        } while (segment != null);

        if (segment == null)
            //TODO throw error
            return DataType.Unknown;

        var dataType = GetDeclaredDataType(context.AnalyzerContext, name);

        if (segment.Parent is { SegmentType: SegmentType.Generic }) variableType = VariableType.Generic;
        else if (segment.Parent is { SegmentType: SegmentType.Record }) variableType = VariableType.RecordMember;

        foreach (var s in names)
        {
            var decl = s.NameOrValue.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var varName = decl.Length > 1 ? decl[1] : s.NameOrValue;
            var off = s.Offset + (decl.Length > 1 ? decl[0].Length + 1 : 0);

            s.DataType = dataType;
            context.AnalyzerContext.UnresolvedSegments.Remove(s);

            var variableOwner =
                GetVariableOwner(context.AnalyzerContext.AvailableTypes, context.CurrentSegment, variableType);

            if (variableOwner is Segment { SegmentType: SegmentType.SeqFunction } seqFunc &&
                AnalyzerHelper.SearchSeqFunction(context.CurrentSegment, seqFunc.LastName.ToLower()) is { } seqOwner)
            {
                if (context.CurrentParsePosition is ParsePosition.Parameter && variableType is VariableType.Unknown) variableType = VariableType.Signal;
                var variable = new DefinedVariable(segment, varName, dataType, variableType, context.CurrentIndex);

                if (!variableOwner.Variables.ContainsKey(varName.ToLower()))
                    variableOwner.Variables.Add(varName.ToLower(), variable);

                if (context.CurrentParsePosition is ParsePosition.Parameter &&
                    AnalyzerHelper.SearchParameterOwner(context.CurrentSegment) is
                        { SegmentType: SegmentType.SeqFunction })
                {
                    seqOwner.Parameters.Add(new FunctionParameter(varName.ToLower(), dataType));
                }
                else
                {
                    //Exposing variables from seqfunction
                    if (!seqOwner.ExposingVariables.ContainsKey(varName.ToLower()))
                        seqOwner.ExposingVariables.Add(varName.ToLower(), variable);
                }
            }
            else
            {
                if (variableOwner is Segment { SegmentType: SegmentType.Function } func &&
                    AnalyzerHelper.SearchFunction(context.CurrentSegment, func.LastName.ToLower()) is
                        { } funcOwner)
                    if (context.CurrentParsePosition is ParsePosition.Parameter &&
                        context.CurrentSegment.Parent != null &&
                        AnalyzerHelper.SearchParameterOwner(context.CurrentSegment.Parent) is
                            { SegmentType: SegmentType.Function })
                        funcOwner.Parameters.Add(new FunctionParameter(varName.ToLower(), dataType));

                if (!variableOwner.Variables.ContainsKey(varName.ToLower()))
                {
                    if (variableType is VariableType.Io)
                    {
                        var variable = new DefinedIo(segment, varName, dataType, variableType, ioType,
                            context.CurrentIndex);

                        variableOwner.Variables.Add(varName.ToLower(), variable);
                    }
                    else
                    {
                        var variable = new DefinedVariable(segment, varName, dataType, variableType,
                            context.CurrentIndex);
                        variableOwner.Variables.Add(varName.ToLower(), variable);
                        if (variableType == VariableType.Constant &&
                            AnalyzerHelper.SearchTopSegment(segment, SegmentType.Package) != null)
                            context.AnalyzerContext.AddLocalExposingVariable(varName.ToLower(), variable);
                        if (variableType is VariableType.Unknown &&
                            context.CurrentSegment.Parent is
                                {SegmentType: SegmentType.Component or SegmentType.Main} &&
                            context.CurrentParsePosition is ParsePosition.Parameter)
                        {
                            context.AnalyzerContext.Diagnostics.Add(new SegmentParserDiagnostic(context.AnalyzerContext,
                                $"I/O Type missing (IN, OUT, BUFFER)", DiagnosticLevel.Error, off,
                                off + varName.Length)); //TODO move outside of segment parser
                        }
                    }
                }
                else
                {
                    context.AnalyzerContext.Diagnostics.Add(new SegmentParserDiagnostic(context.AnalyzerContext,
                        $"{variableType} {varName} already defined in {variableOwner}", DiagnosticLevel.Error, off,
                        off + varName.Length)); //TODO move outside of segment parser
                }
            }
        }

        return dataType;
    }

    public static IVariableOwner GetVariableOwner(IDictionary<string, DataType> types, Segment segment,
        VariableType variableType)
    {
        IVariableOwner variableOwner = segment;

        if (AnalyzerHelper.SearchTopSegment(segment, SegmentType.Record) is
            { Parent: { SegmentType: SegmentType.Type } } record)
        {
            var typeName = record.Parent?.NameOrValue.Split(' ').Last().ToLower() ?? string.Empty;
            if (types.TryGetValue(typeName, out var dt) && dt is CustomDefinedRecord cRecord)
                variableOwner = cRecord;
        }
        else if (AnalyzerHelper.SearchTopSegment(segment, SegmentType.SeqFunction, SegmentType.Function) is { } func)
        {
            variableOwner = func;
        }
        else if (variableType is VariableType.Variable &&
                 AnalyzerHelper.SearchTopSegment(segment, SegmentType.Process) is { } proc)
        {
            variableOwner = proc;
        }
        else if (AnalyzerHelper.SearchTopSegment(segment, SegmentType.Main, SegmentType.Component, SegmentType.Package)
                 is { } top)
        {
            variableOwner = top;
        }

        return variableOwner;
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