﻿using VHDPlus.Analyzer.Checks;
using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer;

public enum AnalyzerMode
{
    Indexing,
    Resolve,
    ErrorCheck,
    Full
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

        context.ResolveIncludes();
        ResolveMissingTypes(context, context.AvailableTypes);
        if (mode is AnalyzerMode.Full or AnalyzerMode.Resolve)
        {
            ResolveMissingSeqFunctions(context);
            ResolveMissingComponents(context);
            ResolveMissingSegments(context);
        }

        if (mode is AnalyzerMode.Full or AnalyzerMode.ErrorCheck) ErrorCheck(context, mode);

        return context;
    }

    private static void ResolveMissingTypes(AnalyzerContext context, IDictionary<string, DataType> customTypes)
    {
        for (var i = 0; i < context.UnresolvedTypes.Count; i++)
        {
            if (context.UnresolvedTypes[i].SegmentType is SegmentType.Return)
            {
                if (!customTypes.ContainsKey(context.UnresolvedTypes[i].LastName)) continue;
                if (context.UnresolvedTypes[i].SegmentType is SegmentType.Return)
                {
                    var owner = ParserHelper.GetVariableOwner(customTypes, context.UnresolvedTypes[i],
                        VariableType.Variable);
                    if (owner is Segment { SegmentType: SegmentType.Function } funct &&
                        AnalyzerHelper.SearchFunction(context.UnresolvedTypes[i], funct.LastName) is { } functOwner)
                        functOwner.ReturnType = customTypes[context.UnresolvedTypes[i].LastName];
                }

                context.UnresolvedTypes.RemoveAt(i);
                i--;
                continue;
            }

            if (!customTypes.ContainsKey(context.UnresolvedTypes[i].NameOrValue.ToLower())) continue;
            context.UnresolvedTypes[i].DataType = customTypes[context.UnresolvedTypes[i].NameOrValue.ToLower()];
            context.UnresolvedTypes[i].SegmentType = SegmentType.TypeUsage;

            var parent = context.UnresolvedTypes[i].Parent;
            if (context.UnresolvedTypes[i].ConcatOperator is ":" ||
                ParserHelper.VhdlIos.Contains(context.UnresolvedTypes[i].ConcatOperator))
            {
                if (parent is { ConcatOperator: ":" }) parent = parent.Parent;
                while (parent != null)
                {
                    parent.DataType = customTypes[context.UnresolvedTypes[i].NameOrValue.ToLower()];
                    if (parent.ConcatOperator != ",") break;
                    parent = parent.Parent;
                }
            }

            if (parent == null || string.IsNullOrEmpty(parent.NameOrValue)) continue; //TODO Error

            var variableType = ParserHelper.GetVariableType(parent.NameOrValue);
            var variableOwner = ParserHelper.GetVariableOwner(customTypes, context.UnresolvedTypes[i],
                variableType);

            var varName = parent.NameOrValue.Split(' ').Last();

            if (variableOwner.Variables.ContainsKey(varName.ToLower()))
                variableOwner.Variables[varName.ToLower()].DataType = parent.DataType;

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
                AnalyzerHelper.SearchFunction(context.UnresolvedTypes[i], func.LastName) is { } funcOwner)
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
            var variable = AnalyzerHelper.SearchVariable(context.UnresolvedSegments[i],
                               context.UnresolvedSegments[i].NameOrValue) ??
                           AnalyzerHelper.SearchVariableInRecord(context.UnresolvedSegments[i]);
            if (variable == null &&
                context.AvailableExposingVariables.ContainsKey(context.UnresolvedSegments[i].NameOrValue.ToLower()))
                variable = context.AvailableExposingVariables[context.UnresolvedSegments[i].NameOrValue.ToLower()];
            if (variable == null)
            {
                var function = AnalyzerHelper.SearchFunction(context.UnresolvedSegments[i],
                    context.UnresolvedSegments[i].NameOrValue);
                if (function == null) continue; //Not found variable or function
                context.UnresolvedSegments[i].SegmentType = SegmentType.VhdlFunction;
                context.UnresolvedSegments[i].DataType = function.ReturnType;
            }
            else
            {
                context.UnresolvedSegments[i].DataType = variable.DataType;
                if (context.UnresolvedSegments[i].SegmentType is SegmentType.Unknown)
                    context.UnresolvedSegments[i].SegmentType = SegmentType.DataVariable;
            }

            context.UnresolvedSegments.RemoveAt(i);
            i--;
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
                                        context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context,
                                            $"{newName} already defined in {variableOwner}", DiagnosticLevel.Error,
                                            funcPar.Offset,
                                            funcPar.Offset + newName.Length));
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

    private static void ErrorCheck(AnalyzerContext context, AnalyzerMode mode)
    {
        foreach (var s in context.UnresolvedSegments)
            context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context, $"Undefined Variable {s.NameOrValue}",
                DiagnosticLevel.Error, s));

        foreach (var s in context.UnresolvedComponents)
            context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context, $"Undefined Component {s.LastName}",
                DiagnosticLevel.Error, s));

        foreach (var s in context.UnresolvedSeqFunctions)
            context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context, $"Undefined SeqFunction {s.LastName}",
                DiagnosticLevel.Error, s));

        foreach (var s in context.UnresolvedTypes)
            context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context, $"Undefined Type {s.NameOrValue}",
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