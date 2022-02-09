using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Checks;

public class OperatorCheck
{
    public static void CheckSegmentPair(Segment parent, Segment child, AnalyzerContext context, IDictionary<DefinedVariable, Segment> constantDrivers)
    {
        switch (child.ConcatOperator)
        {
            case "=":
                if (AnalyzerHelper.SearchConcatParent(parent) is
                    {
                        SegmentType: SegmentType.DataVariable or SegmentType.VariableDeclaration
                    } recordParent0 && AnalyzerHelper.SearchVariable(recordParent0) is DefinedIo
                    {
                        IoType: IoType.Out
                    } variableEquals)
                    context.Diagnostics.Add(new OperatorCheckDiagnostic(context,
                        $"Invalid Operator '=' for {variableEquals.IoType} {parent}. Cannot read from Output, use Buffer instead",
                        DiagnosticLevel.Error, child.ConcatOperatorIndex,
                        child.ConcatOperatorIndex + child.ConcatOperator.Length));
                break;
            case ":=":
                if (AnalyzerHelper.SearchConcatParent(parent) is
                        { SegmentType: SegmentType.DataVariable } recordParent1 &&
                    AnalyzerHelper.SearchVariable(recordParent1) is
                    {
                        VariableType: VariableType.Io or VariableType.Signal or VariableType.RecordMember
                    } variableIo)
                    context.Diagnostics.Add(new OperatorCheckDiagnostic(context,
                        $"Invalid Operator := for {variableIo.VariableType} {parent}. Use <= instead",
                        DiagnosticLevel.Error, child.ConcatOperatorIndex,
                        child.ConcatOperatorIndex + child.ConcatOperator.Length));
                break;
            case "<=" when !AnalyzerHelper.InParameter(parent):
                if (AnalyzerHelper.SearchConcatParent(parent) is
                    {
                        SegmentType: SegmentType.DataVariable or SegmentType.VariableDeclaration,
                        ConcatOperator: not ("and" or "or" or "when")
                    } recordParent2 && AnalyzerHelper.SearchVariable(recordParent2) is { } variable)
                {
                    if (variable.VariableType is not (VariableType.Io or VariableType.Signal
                        or VariableType.Unknown))
                        context.Diagnostics.Add(new OperatorCheckDiagnostic(context,
                            $"Invalid Operator <= for {variable.VariableType} {parent}. Use := instead",
                            DiagnosticLevel.Error, child.ConcatOperatorIndex,
                            child.ConcatOperatorIndex + child.ConcatOperator.Length));
                    else if (variable is DefinedIo { IoType: IoType.In })
                        context.Diagnostics.Add(new OperatorCheckDiagnostic(context,
                            $"Invalid Operator <= for {variable.VariableType} {parent}. Cannot assign a value to an input",
                            DiagnosticLevel.Error, child.ConcatOperatorIndex,
                            child.ConcatOperatorIndex + child.ConcatOperator.Length));

                    if (variable.VariableType is VariableType.Signal or VariableType.Io &&
                        variable.DataType is not CustomDefinedRecord && !recordParent2.Parameter.Any())
                        if (AnalyzerHelper.SearchTopSegment(parent, SegmentType.Process, SegmentType.Main,
                                SegmentType.Component) is { } topLevel)
                        {
                            if (constantDrivers.ContainsKey(variable))
                            {
                                if ((constantDrivers[variable] != topLevel ||
                                     topLevel.SegmentType is not SegmentType.Process) &&
                                    AnalyzerHelper.SearchTopSegment(parent, SegmentType.Generate) == null)
                                    context.Diagnostics.Add(new OperatorCheckDiagnostic(context,
                                        $"Multiple constant drivers for {parent}. You can only drive a signal from one process",
                                        DiagnosticLevel.Error, child.ConcatOperatorIndex,
                                        child.ConcatOperatorIndex + child.ConcatOperator.Length));
                            }
                            else
                            {
                                constantDrivers.Add(variable, topLevel);
                            }
                        }
                }
                break;
        }
    }
}