using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Checks;

public class OperatorCheck
{
    public static void CheckSegments(AnalyzerContext context)
    {
        CheckSegments(context, context.TopSegment.Children, new Dictionary<DefinedVariable, Segment>());
    }
    
    private static void CheckSegments(AnalyzerContext context, IEnumerable<Segment> segments, Dictionary<DefinedVariable, Segment> constantDrivers, bool thread = false)
    {
        foreach (var segment in segments)
        {
            if (segment.SegmentType is SegmentType.Unknown or SegmentType.Vhdl) continue;
            
            foreach (var child in segment.Children)
            {
                if(child.SegmentType is SegmentType.Unknown) continue;
                switch (child.ConcatOperator)
                { 
                    case "=":
                        if (AnalyzerHelper.SearchConcatParent(segment) is {SegmentType: SegmentType.DataVariable or SegmentType.VariableDeclaration} recordParent0 && AnalyzerHelper.SearchVariable(recordParent0) is DefinedIo { IoType: IoType.Out } variableEquals)
                        {
                            context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context,
                                $"Invalid Operator '=' for {variableEquals.IoType} {segment}. Cannot read from Output, use Buffer instead", DiagnosticLevel.Error, child.ConcatOperatorIndex, child.ConcatOperatorIndex + child.ConcatOperator.Length));
                        }
                        break;
                    case ":=":
                        if (AnalyzerHelper.SearchConcatParent(segment) is {SegmentType: SegmentType.DataVariable} recordParent1  && AnalyzerHelper.SearchVariable(recordParent1) is { VariableType: VariableType.Io or VariableType.Signal or VariableType.RecordMember } variableIo)
                        {
                            context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context,
                                $"Invalid Operator := for {variableIo.VariableType} {segment}. Use <= instead", DiagnosticLevel.Error, child.ConcatOperatorIndex, child.ConcatOperatorIndex + child.ConcatOperator.Length));
                        }
                        break;
                    case "<=" when !AnalyzerHelper.InParameter(segment):
                        if (AnalyzerHelper.SearchConcatParent(segment) is {SegmentType: SegmentType.DataVariable or SegmentType.VariableDeclaration, ConcatOperator: not ("and" or "or" or "when")} recordParent2  && AnalyzerHelper.SearchVariable(recordParent2) is {} variable)
                        {
                            if (variable.VariableType is not (VariableType.Io or VariableType.Signal
                                or VariableType.Unknown))
                            {
                                context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context,
                                    $"Invalid Operator <= for {variable.VariableType} {segment}. Use := instead", DiagnosticLevel.Error, child.ConcatOperatorIndex, child.ConcatOperatorIndex + child.ConcatOperator.Length));
                            }
                            else if (variable is DefinedIo { IoType: IoType.In })
                            {
                                context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context,
                                    $"Invalid Operator <= for {variable.VariableType} {segment}. Cannot assign a value to an input", DiagnosticLevel.Error, child.ConcatOperatorIndex, child.ConcatOperatorIndex + child.ConcatOperator.Length));
                            }

                            if (variable.VariableType is VariableType.Signal or VariableType.Io && variable.DataType is not CustomDefinedRecord && !recordParent2.Parameter.Any())
                            {
                                if (AnalyzerHelper.SearchTopSegment(segment, SegmentType.Process, SegmentType.Main, SegmentType.Component) is { } topLevel)
                                {
                                    if (constantDrivers.ContainsKey(variable))
                                    {
                                        if((constantDrivers[variable] != topLevel || topLevel.SegmentType is not SegmentType.Process) && AnalyzerHelper.SearchTopSegment(segment, SegmentType.Generate) == null)
                                            context.Diagnostics.Add(new GenericAnalyzerDiagnostic(context,
                                                $"Multiple constant drivers for {segment}. You can only drive a signal from one process", DiagnosticLevel.Error, child.ConcatOperatorIndex, child.ConcatOperatorIndex + child.ConcatOperator.Length));
                                    }
                                    else constantDrivers.Add(variable, topLevel);
                                }
                            }
                        }
                        break;
                }
            }
            CheckSegments(context, segment.Children, constantDrivers, thread || segment.SegmentType is SegmentType.Thread);

            foreach (var par in segment.Parameter)
            {
                CheckSegments(context, par, constantDrivers, thread || segment.SegmentType is SegmentType.Thread);
            }
        }
    }
}