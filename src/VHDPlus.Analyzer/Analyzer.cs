using VHDPlus.Analyzer.Checks;
using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer;

[Flags]
public enum AnalyzerMode
{
    Indexing,
    Resolve,
    Check,
}

public static class Analyzer
{
    public static AnalyzerContext Analyze(string path, string content, AnalyzerMode mode, ProjectContext? pC = null)
    {
        var context = SegmentParser.Parse(path, content);

        return Analyze(context, mode, pC);
    }

    public static AnalyzerContext Analyze(AnalyzerContext context, AnalyzerMode mode, ProjectContext? pC)
    {
        if (pC != null) context.AddProjectContext(pC);

        if (mode.HasFlag(AnalyzerMode.Resolve) || mode.HasFlag(AnalyzerMode.Check))
        {
            //Filter out all diagnostics that are not from segment parsing 
            context.Diagnostics.RemoveAll(x => x is not (SegmentParserDiagnostic or ResolveDiagnostic));
        }

        if (mode.HasFlag(AnalyzerMode.Resolve))
        {
            context.Diagnostics.RemoveAll(x => x is ResolveDiagnostic);

            context.ResolveIncludes();
            ResolveMissingTypes(context, context.AvailableTypes);
            ResolveMissingSeqFunctions(context);
            ResolveMissingComponents(context);
            ResolveMissingSegments(context);
        }
        if (mode.HasFlag(AnalyzerMode.Check))
        {
            ErrorCheck(context);
        }
        return context;
    }

    private static void ResolveMissingTypes(AnalyzerContext context, IDictionary<string, DataType> customTypes)
    {
        for (var i = 0; i < context.UnresolvedTypes.Count; i++)
        {
            if (context.UnresolvedTypes[i].ConcatOperator is "return")
            {
                if (!customTypes.ContainsKey(context.UnresolvedTypes[i].LastName.ToLower())) continue;
                var owner = ParserHelper.GetVariableOwner(customTypes, context.UnresolvedTypes[i],
                    VariableType.Variable);
                if (owner is Segment { SegmentType: SegmentType.Function } funct &&
                    AnalyzerHelper.SearchFunction(funct, funct.LastName) is { } functOwner)
                    functOwner.ReturnType = customTypes[context.UnresolvedTypes[i].LastName.ToLower()];

                context.UnresolvedTypes.RemoveAt(i);
                i--;
                continue;
            }

            if (!customTypes.ContainsKey(context.UnresolvedTypes[i].NameOrValue.ToLower())) continue;
            context.UnresolvedTypes[i].DataType = customTypes[context.UnresolvedTypes[i].NameOrValue.ToLower()];
            context.UnresolvedTypes[i].SegmentType = SegmentType.TypeUsage;
            if(context.UnresolvedTypes[i].Parent is {SegmentType: SegmentType.SubType} subTypeParent && customTypes.ContainsKey(subTypeParent.LastName.ToLower()))
            { 
                //customTypes[subTypeParent.LastName.ToLower()] = subTypeParent.DataType;
            }

            if (context.UnresolvedTypes[i].Parent is {SegmentType: SegmentType.Array, Parent: not null} arrayParent)
            {
                if (context.AvailableTypes.TryGetValue(arrayParent.Parent.LastName.ToLower(), out var type))
                {
                    if (type is CustomDefinedArray array)
                    {
                        array.ArrayType = customTypes[context.UnresolvedTypes[i].NameOrValue.ToLower()];
                    }
                }
            }

            var parent = context.UnresolvedTypes[i].Parent;

            List<string> varNames = new List<string>();
            if (context.UnresolvedTypes[i].ConcatOperator is ":" ||
                ParserHelper.VhdlIos.Contains(context.UnresolvedTypes[i].ConcatOperator))
            {
                if (parent is { ConcatOperator: ":" }) parent = parent.Parent;
                while (parent != null)
                {
                    parent.DataType = customTypes[context.UnresolvedTypes[i].NameOrValue.ToLower()];
                    varNames.Add(parent.LastName.ToLower());
                    if (parent.ConcatOperator != ",") break;
                    parent = parent.Parent;
                }
            }

            if (parent == null || string.IsNullOrEmpty(parent.NameOrValue)) continue; //TODO Error

            var variableType = ParserHelper.GetVariableType(parent.NameOrValue);
            var variableOwner = ParserHelper.GetVariableOwner(customTypes, context.UnresolvedTypes[i],
                variableType);

            var varName = parent.NameOrValue.Split(' ').Last();

            foreach (var n in varNames)
            {
                if (variableOwner.Variables.ContainsKey(n))
                    variableOwner.Variables[n].DataType = parent.DataType;
            }
            
            if (variableOwner is Segment { SegmentType: SegmentType.SeqFunction } seqFunc &&
                AnalyzerHelper.SearchSeqFunction(context.UnresolvedTypes[i], seqFunc.LastName) is { } seqOwner)
            {
                var par = seqOwner.Parameters.FirstOrDefault(x =>
                    x.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));
                if (par != null)
                {
                    if (seqOwner.ExposingVariables.ContainsKey(par.Name.ToLower()))
                        seqOwner.ExposingVariables[par.Name.ToLower()].DataType = parent.DataType;
                    par.DataType = parent.DataType;
                }
            }

            if (variableOwner is Segment { SegmentType: SegmentType.Function } func &&
                AnalyzerHelper.SearchFunction(func, func.LastName) is { } funcOwner)
            {
                var par = funcOwner.Parameters.FirstOrDefault(x =>
                    x.Name.Equals(varName, StringComparison.OrdinalIgnoreCase));
                if (par != null) par.DataType = parent.DataType;
            }

            context.UnresolvedTypes.RemoveAt(i);
            i--;
        }
    }

    private static void ResolveMissingSegments(AnalyzerContext context)
    {
        for (var i = 0; i < context.UnresolvedSegments.Count; i++)
        {
            var nL = context.UnresolvedSegments[i].NameOrValue.ToLower();
            
            //Search in record or defined variable
            var variable = context.UnresolvedSegments[i].ConcatOperator is "." ? AnalyzerHelper.SearchVariableInRecord(context.UnresolvedSegments[i]) : 
                    AnalyzerHelper.SearchVariable(context.UnresolvedSegments[i], context.UnresolvedSegments[i].NameOrValue);

            //Search in exposing variables
            if (variable == null)
                context.AvailableExposingVariables.TryGetValue(nL, out variable);

            if (variable != null)
            {
                context.UnresolvedSegments[i].DataType = variable.DataType;
                if (context.UnresolvedSegments[i].SegmentType is SegmentType.Unknown)
                    context.UnresolvedSegments[i].SegmentType = SegmentType.DataVariable;
                
                context.UnresolvedSegments.RemoveAt(i);
                i--;
                continue;
            }

            //Search for function
            var function = AnalyzerHelper.SearchFunction(context.UnresolvedSegments[i],
                context.UnresolvedSegments[i].NameOrValue);

            if (function != null)
            {
                context.UnresolvedSegments[i].SegmentType = SegmentType.VhdlFunction;
                context.UnresolvedSegments[i].DataType = function.ReturnType;
                
                context.UnresolvedSegments.RemoveAt(i);
                i--;
                continue;
            }
            
                        
            //Search in enum
            var en= AnalyzerHelper.SearchEnum(context, nL);
            if (en != null)
            {
                context.UnresolvedSegments[i].SegmentType = SegmentType.DataVariable;
                context.UnresolvedSegments[i].DataType = en;
                
                context.UnresolvedSegments.RemoveAt(i);
                i--;
                continue;
            }
        }
    }

    private static void ResolveMissingComponents(AnalyzerContext context)
    {
        for (var i = 0; i < context.UnresolvedComponents.Count; i++)
            if (context.UnresolvedComponents[i].NameOrValue.Split(' ') is { Length: 2 } comp)
            {
                var compName = comp[1].ToLower();
                if (context.AvailableComponents.ContainsKey(compName))
                {
                    var component = context.AvailableComponents[compName];

                    if (context.UnresolvedComponents[i].Parameter.Any())
                        foreach (var componentMember in context.UnresolvedComponents[i].Parameter.First())
                            if (component.Variables.ContainsKey(componentMember.NameOrValue.ToLower()))
                            {
                                var variable = component.Variables[componentMember.NameOrValue.ToLower()];
                                componentMember.DataType = variable.DataType;
                            }

                    context.UnresolvedComponents.RemoveAt(i);
                    i--;
                }
            }
    }

    private static void ResolveMissingSeqFunctions(AnalyzerContext context)
    {
        for (var i = 0; i < context.UnresolvedSeqFunctions.Count; i++)
            if (context.UnresolvedSeqFunctions[i].NameOrValue.Split(' ') is { Length: 2 } comp)
            {
                var compName = comp[1];
                if (AnalyzerHelper.SearchSeqFunction(context.UnresolvedSeqFunctions[i], compName) is { } seqFunc)
                {
                    //Expose variables
                    foreach (var seqVariable in seqFunc.ExposingVariables)
                        if (seqFunc.Parameters.FirstOrDefault(x => x.Name == seqVariable.Key) is { } parameter &&
                            context.UnresolvedSeqFunctions[i].Parameter.Any())
                        {
                            var variableOwner = ParserHelper.GetVariableOwner(context.AvailableTypes,
                                context.UnresolvedSeqFunctions[i],
                                seqVariable.Value.VariableType);

                            var parIndex = seqFunc.Parameters.IndexOf(parameter);
                            var funcPar = context.UnresolvedSeqFunctions[i].Parameter[0].Any()
                                ? context.UnresolvedSeqFunctions[i].Parameter[0][0]
                                : null;
                            for (var c = 0; funcPar != null; c++)
                            {
                                if (c == parIndex)
                                {
                                    funcPar.DataType = seqVariable.Value.DataType;
                                    funcPar.SegmentType = SegmentType.VariableDeclaration;
                                    var newName = funcPar.NameOrValue;
                                    if (!variableOwner.Variables.ContainsKey(newName.ToLower()))
                                        variableOwner.Variables.Add(newName.ToLower(),
                                            new DefinedVariable(context.UnresolvedSeqFunctions[i], newName,
                                                seqVariable.Value.DataType, seqVariable.Value.VariableType,
                                                context.UnresolvedSeqFunctions[i].Offset));
                                    else
                                        context.Diagnostics.Add(new ResolveDiagnostic(context, 
                                            $"{newName} already defined in {variableOwner}", DiagnosticLevel.Error,
                                            funcPar.Offset,
                                            funcPar.Offset + newName.Length)); //TODO change diagnostic type
                                    break;
                                }

                                funcPar = AnalyzerHelper.SearchNextOperatorChild(funcPar, ",");
                                if (funcPar == null) break;
                            }

                            if (funcPar == null)
                            {
                                if (!variableOwner.Variables.ContainsKey(seqVariable.Key))
                                    variableOwner.Variables.Add(seqVariable.Key, seqVariable.Value);
                                else
                                    context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context,
                                        $"{seqVariable.Key} already defined in {variableOwner}", DiagnosticLevel.Error,
                                        context.UnresolvedSeqFunctions[i].Offset,
                                        context.UnresolvedSeqFunctions[i].Offset +
                                        context.UnresolvedSeqFunctions[i].NameOrValue.Length));
                            }
                        }

                    context.UnresolvedSeqFunctions.RemoveAt(i);
                    i--;
                }
            }
    }

    private static void ErrorCheck(AnalyzerContext context)
    {
        foreach (var s in context.UnresolvedSegments)
            context.Diagnostics.Add(new MissingComponentDiagnostic(context, $"Undefined Variable {s.NameOrValue}",
                DiagnosticLevel.Error, s));

        foreach (var s in context.UnresolvedComponents)
            context.Diagnostics.Add(new MissingComponentDiagnostic(context, $"Undefined Component {s.LastName}",
                DiagnosticLevel.Error, s));

        foreach (var s in context.UnresolvedSeqFunctions)
            context.Diagnostics.Add(new MissingComponentDiagnostic(context, $"Undefined SeqFunction {s.LastName}",
                DiagnosticLevel.Error, s));

        foreach (var s in context.UnresolvedTypes)
            context.Diagnostics.Add(new MissingComponentDiagnostic(context, $"Undefined Type {s.LastName}",
                DiagnosticLevel.Warning, s));


        var constantDrivers = new Dictionary<DefinedVariable, Segment>();
        SegmentCrawler.GetPairs(context.TopSegment, (parent, child, parameter, thread) =>
        {
            SegmentCheck.CheckSegmentPair(parent, child, context, parameter, thread);
            OperatorCheck.CheckSegmentPair(parent, child, context, constantDrivers);
            TypeCheck.CheckTypePair(parent, child, context);
            
            if (parent.SegmentType is SegmentType.VhdlFunction or SegmentType.NewFunction or SegmentType.CustomBuiltinFunction)
                TypeCheck.CheckFunctionParameter(context, parent);
        });
    }
}