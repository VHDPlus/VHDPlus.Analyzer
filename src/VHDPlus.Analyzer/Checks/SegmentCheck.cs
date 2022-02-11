using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Checks;

public static class SegmentCheck
{
    private static readonly Dictionary<SegmentType, IEnumerable<SegmentType>> ValidSegments = new();
    private static readonly Dictionary<SegmentType, IEnumerable<SegmentType>> ValidSegmentsThread = new();
    private static readonly Dictionary<SegmentType, IEnumerable<SegmentType>> ValidSegmentsParameter = new();

    static SegmentCheck()
    {
        foreach (var segment in Enum.GetValues(typeof(SegmentType)).Cast<SegmentType>())
        {
            ValidSegments.Add(segment, GetValidSegments(segment));
            ValidSegmentsThread.Add(segment, GetValidSegmentsThread(segment));
            ValidSegmentsParameter.Add(segment, GetValidSegmentsParameter(segment));
        }
    }

    public static void CheckSegmentPair(Segment parent, Segment child, AnalyzerContext context, bool parameter, bool thread)
    {
        if (child.SegmentType is SegmentType.Unknown || parent.SegmentType is SegmentType.Unknown || child.ConcatOperator is "," or "=>") return;
        if (parameter)
        {
            if (ValidSegmentsParameter[parent.SegmentType].Contains(child.SegmentType)) return;
            context.Diagnostics.Add(new SegmentCheckDiagnostic(context,
                $"Invalid Parameter Segment {child.SegmentType} at {parent.SegmentType}",
                DiagnosticLevel.Warning, child));
        }
        else if (thread)
        {
            if (ValidSegmentsThread[parent.SegmentType].Contains(child.SegmentType)) return;
            context.Diagnostics.Add(new SegmentCheckDiagnostic(context,
                $"Invalid Segment {child.SegmentType} inside Thread at {parent.SegmentType}",
                DiagnosticLevel.Warning, child));
        }
        else
        {
            if (ValidSegments[parent.SegmentType].Contains(child.SegmentType)) return;

            context.Diagnostics.Add(new SegmentCheckDiagnostic(context,
                $"Invalid Segment {child.SegmentType} at {parent.SegmentType}", DiagnosticLevel.Warning,
                child));
        }
    }
    
    private static IEnumerable<SegmentType> GetValidSegments(SegmentType type)
    {
        yield return SegmentType.Vhdl;
        yield return SegmentType.EmptyName;

        switch (type)
        {
            case SegmentType.Include:
            case SegmentType.IncludePackage:
                yield return SegmentType.IncludePackage;
                break;
            case SegmentType.Package:
                yield return SegmentType.Include;
                yield return SegmentType.VariableDeclaration;
                yield return SegmentType.Type;
                yield return SegmentType.SubType;
                yield return SegmentType.SeqFunction;
                yield return SegmentType.VhdlEnd;
                yield return SegmentType.Vhdl;
                break;
            case SegmentType.AttributeDeclaration:
                yield return SegmentType.VariableDeclaration;
                yield return SegmentType.Type;
                yield return SegmentType.SubType;
                yield return SegmentType.Function;
                yield return SegmentType.Vhdl;
                break;
            case SegmentType.GlobalSegment:
                yield return SegmentType.Main;
                yield return SegmentType.Component;
                yield return SegmentType.Package;
                break;
            case SegmentType.Main:
            case SegmentType.Component:
            case SegmentType.Generate:
                yield return SegmentType.Type;
                yield return SegmentType.SubType;
                yield return SegmentType.VariableDeclaration;
                yield return SegmentType.DataVariable;
                yield return SegmentType.Function;
                yield return SegmentType.Process;
                yield return SegmentType.AttributeDeclaration;
                yield return SegmentType.SeqFunction;
                yield return SegmentType.Connections;
                yield return SegmentType.NewComponent;
                yield return SegmentType.Generate;
                yield return SegmentType.With;
                yield return SegmentType.VhdlEnd;
                break;
            case SegmentType.Function:
            case SegmentType.For:
            case SegmentType.While:
                yield return SegmentType.If;
                yield return SegmentType.Elsif;
                yield return SegmentType.Else;
                yield return SegmentType.Case;
                yield return SegmentType.Type;
                yield return SegmentType.SubType;
                yield return SegmentType.VariableDeclaration;
                yield return SegmentType.DataVariable;
                yield return SegmentType.Return;
                yield return SegmentType.For;
                yield return SegmentType.ParFor;
                yield return SegmentType.ParWhile;
                yield return SegmentType.While;
                yield return SegmentType.Null;
                yield return SegmentType.Exit;
                yield return SegmentType.VhdlFunction;
                break;
            case SegmentType.Process:
            case SegmentType.Else:
            case SegmentType.Elsif:
            case SegmentType.If:
            case SegmentType.When:
                yield return SegmentType.If;
                yield return SegmentType.Elsif;
                yield return SegmentType.Else;
                yield return SegmentType.Case;
                yield return SegmentType.Type;
                yield return SegmentType.SubType;
                yield return SegmentType.VariableDeclaration;
                yield return SegmentType.DataVariable;
                yield return SegmentType.Function;
                yield return SegmentType.For;
                yield return SegmentType.While;
                yield return SegmentType.ParFor;
                yield return SegmentType.ParWhile;
                yield return SegmentType.Null;
                yield return SegmentType.Thread;
                yield return SegmentType.Exit;
                yield return SegmentType.VhdlFunction;
                break;
            case SegmentType.Case:
                yield return SegmentType.When;
                yield return SegmentType.DataVariable;
                break;
            case SegmentType.VariableDeclaration:
                yield return SegmentType.TypeUsage;
                break;
            case SegmentType.DataVariable:
            case SegmentType.NativeDataValue:
            case SegmentType.TypeUsage:
            case SegmentType.VhdlAttribute:
                yield return SegmentType.EmptyName;
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.DataVariable;
                yield return SegmentType.VhdlFunction;
                yield return SegmentType.VhdlAttribute;
                yield return SegmentType.None;
                break;
            case SegmentType.EmptyName:
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.DataVariable;
                yield return SegmentType.VhdlFunction;
                yield return SegmentType.TypeUsage;
                break;
            case SegmentType.VhdlFunction:
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.DataVariable;
                yield return SegmentType.VhdlFunction;
                break;
            case SegmentType.Type:
                yield return SegmentType.Record;
                yield return SegmentType.Array;
                yield return SegmentType.DataVariable;
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.EnumDeclaration;
                break;
            case SegmentType.Record:
                yield return SegmentType.VariableDeclaration;
                break;
            case SegmentType.Array:
                yield return SegmentType.TypeUsage;
                break;
            case SegmentType.With:
                yield return SegmentType.DataVariable;
                break;
            case SegmentType.Vhdl:
            case SegmentType.Begin:
                yield return SegmentType.If;
                yield return SegmentType.Elsif;
                yield return SegmentType.Else;
                yield return SegmentType.Case;
                yield return SegmentType.Type;
                yield return SegmentType.SubType;
                yield return SegmentType.VariableDeclaration;
                yield return SegmentType.DataVariable;
                yield return SegmentType.Function;
                yield return SegmentType.Process;
                yield return SegmentType.For;
                yield return SegmentType.While;
                yield return SegmentType.AttributeDeclaration;
                yield return SegmentType.SeqFunction;
                yield return SegmentType.Connections;
                yield return SegmentType.NewComponent;
                yield return SegmentType.Generate;
                yield return SegmentType.With;
                yield return SegmentType.VhdlEnd;
                yield return SegmentType.Attribute;
                break;
            case SegmentType.Connections:
            case SegmentType.ConnectionsMember:
                yield return SegmentType.ConnectionsMember;
                break;
        }
    }

    private static IEnumerable<SegmentType> GetValidSegmentsThread(SegmentType type)
    {
        yield return SegmentType.Vhdl;
        yield return SegmentType.EmptyName;

        switch (type)
        {
            case SegmentType.Function:
            case SegmentType.For:
            case SegmentType.While:
                yield return SegmentType.If;
                yield return SegmentType.Elsif;
                yield return SegmentType.Else;
                yield return SegmentType.Case;
                yield return SegmentType.Type;
                yield return SegmentType.SubType;
                yield return SegmentType.VariableDeclaration;
                yield return SegmentType.DataVariable;
                yield return SegmentType.Return;
                yield return SegmentType.For;
                yield return SegmentType.While;
                yield return SegmentType.SeqWhile;
                yield return SegmentType.SeqFor;
                yield return SegmentType.ParWhile;
                yield return SegmentType.ParFor;
                yield return SegmentType.Step;
                yield return SegmentType.NewFunction;
                yield return SegmentType.CustomBuiltinFunction;
                break;
            case SegmentType.Case:
                yield return SegmentType.When;
                break;
            case SegmentType.VariableDeclaration:
                yield return SegmentType.TypeUsage;
                break;
            case SegmentType.DataVariable:
            case SegmentType.NativeDataValue:
            case SegmentType.TypeUsage:
            case SegmentType.VhdlAttribute:
                yield return SegmentType.EmptyName;
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.DataVariable;
                yield return SegmentType.VhdlFunction;
                yield return SegmentType.VhdlAttribute;
                break;
            case SegmentType.EmptyName:
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.DataVariable;
                yield return SegmentType.VhdlFunction;
                yield return SegmentType.TypeUsage;
                break;
            case SegmentType.VhdlFunction:
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.DataVariable;
                break;
            case SegmentType.Type:
                yield return SegmentType.Record;
                yield return SegmentType.Array;
                yield return SegmentType.DataVariable;
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.EnumDeclaration;
                break;
            case SegmentType.Record:
                yield return SegmentType.VariableDeclaration;
                break;
            case SegmentType.Array:
                yield return SegmentType.TypeUsage;
                break;
            case SegmentType.Thread:
            case SegmentType.If:
            case SegmentType.Elsif:
            case SegmentType.Else:
            case SegmentType.Step:
            case SegmentType.SeqFor:
            case SegmentType.When:
            case SegmentType.FunctionContent:
            case SegmentType.ParFor:
                yield return SegmentType.CustomBuiltinFunction;
                yield return SegmentType.If;
                yield return SegmentType.Elsif;
                yield return SegmentType.Else;
                yield return SegmentType.Case;
                yield return SegmentType.TypeUsage;
                yield return SegmentType.VariableDeclaration;
                yield return SegmentType.DataVariable;
                yield return SegmentType.Function;
                yield return SegmentType.For;
                yield return SegmentType.Step;
                yield return SegmentType.While;
                yield return SegmentType.NewFunction;
                yield return SegmentType.Null;
                yield return SegmentType.VhdlFunction;
                yield return SegmentType.SeqWhile;
                yield return SegmentType.SeqFor;
                yield return SegmentType.ParWhile;
                yield return SegmentType.ParFor;
                break;
            case SegmentType.SeqFunction:
                yield return SegmentType.CustomBuiltinFunction;
                yield return SegmentType.If;
                yield return SegmentType.Elsif;
                yield return SegmentType.Else;
                yield return SegmentType.Case;
                yield return SegmentType.TypeUsage;
                yield return SegmentType.VariableDeclaration;
                yield return SegmentType.DataVariable;
                yield return SegmentType.Function;
                yield return SegmentType.For;
                yield return SegmentType.Step;
                yield return SegmentType.While;
                yield return SegmentType.SeqWhile;
                yield return SegmentType.SeqFor;
                yield return SegmentType.ParWhile;
                yield return SegmentType.ParFor;
                yield return SegmentType.NewComponent;
                yield return SegmentType.FunctionContent;
                yield return SegmentType.NewFunction;
                break;
        }
    }

    private static IEnumerable<SegmentType> GetValidSegmentsParameter(SegmentType type)
    {
        yield return SegmentType.EmptyName;
        switch (type)
        {
            case SegmentType.Include:
                yield return SegmentType.IncludePackage;
                break;
            case SegmentType.Component:
                yield return SegmentType.Generic;
                yield return SegmentType.Package;
                yield return SegmentType.Include;
                yield return SegmentType.VariableDeclaration;
                break;
            case SegmentType.Main:
                yield return SegmentType.Package;
                yield return SegmentType.Include;
                yield return SegmentType.VariableDeclaration;
                break;
            case SegmentType.Package:
                yield return SegmentType.Type;
                yield return SegmentType.SubType;
                yield return SegmentType.VariableDeclaration;
                break;
            case SegmentType.Function:
                yield return SegmentType.Return;
                break;
            case SegmentType.For:
            case SegmentType.While:
            case SegmentType.SeqFor:
            case SegmentType.SeqWhile:
                yield return SegmentType.DataVariable;
                yield return SegmentType.VariableDeclaration;
                yield return SegmentType.NativeDataValue;
                break;
            case SegmentType.NewComponent:
                yield return SegmentType.ComponentMember;
                break;
            case SegmentType.Process:
                yield return SegmentType.VariableDeclaration;
                break;
            case SegmentType.DataVariable:
            case SegmentType.NativeDataValue:
            case SegmentType.VhdlFunction:
            case SegmentType.If:
            case SegmentType.Elsif:
            case SegmentType.Case:
            case SegmentType.When:
            case SegmentType.With:
            case SegmentType.Return:
                yield return SegmentType.VhdlFunction;
                yield return SegmentType.DataVariable;
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.Null;
                break;
            case SegmentType.TypeUsage:
            case SegmentType.Array:
                yield return SegmentType.VhdlFunction;
                yield return SegmentType.DataVariable;
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.Range;
                break;
            case SegmentType.EmptyName:
            case SegmentType.NewFunction:
                yield return SegmentType.VhdlFunction;
                yield return SegmentType.DataVariable;
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.VariableDeclaration;
                break;
            case SegmentType.EnumDeclaration:
                yield return SegmentType.Enum;
                break;
            case SegmentType.Generate:
                yield return SegmentType.If;
                yield return SegmentType.For;
                break;
            case SegmentType.Connections:
            case SegmentType.ConnectionsMember:
                yield return SegmentType.ConnectionsMember;
                break;
            case SegmentType.SeqFunction:
                yield return SegmentType.VariableDeclaration;
                break;
            case SegmentType.CustomBuiltinFunction:
                yield return SegmentType.NativeDataValue;
                yield return SegmentType.DataVariable;
                break;
            case SegmentType.ComponentMember:
                yield return SegmentType.DataVariable;
                yield return SegmentType.NativeDataValue;
                break;
            case SegmentType.Generic:
                yield return SegmentType.VariableDeclaration;
                break;
        }
    }

    public static IEnumerable<SegmentType> GetValidSegments(Segment start, bool parameter)
    {
        if (parameter) return GetValidSegmentsParameter(start.SegmentType);
        return AnalyzerHelper.SearchTopSegment(start, SegmentType.Thread, SegmentType.SeqFunction) == null
            ? GetValidSegments(start.SegmentType)
            : GetValidSegmentsThread(start.SegmentType);
    }
}