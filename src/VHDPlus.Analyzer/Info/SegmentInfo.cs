using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Info;

public static class SegmentInfo
{
    private static readonly Dictionary<SegmentType, string> Info = new()
    {
        { SegmentType.Main, "Area in that the main code is written and components are connected" },
        { SegmentType.Class, "Area in that Functions are declared" },
        {
            SegmentType.Component,
            "Component with in- and outputs that can be added to main and other components\n(in- and outputs) e.g. (EN : IN STD_LOGIC; LED : OUT STD_LOGIC)\nComponent [Name] ("
        },
        { SegmentType.AttributeDeclaration, "Area in that signals can be declared" },
        {
            SegmentType.NewComponent,
            "Instance of a component \nI/Os and parameters are connected like this: (EN => EN, LED => LED)"
        },
        {
            SegmentType.Process,
            "Process in that the operations happen\nIn brackets are variables for that Process e.g. VARIABLE LED : STD_LOGIC := '0';"
        },
        { SegmentType.Vhdl, "Area in that normal VHDL code can be written" },
        { SegmentType.Generic, "Area for parameters for the Component (e.g. CLK_Frequency : NATURAL;)" },
        { SegmentType.If, "Runs operation if condition met" },
        { SegmentType.Else, "Runs operation if condition of If not met" },
        { SegmentType.Elsif, "Runs operation if condition of If not met but the own condition" },
        { SegmentType.Case, "Runs operation depending on the parameter" },
        { SegmentType.SeqWhile, "Runs operation while condition met" },
        {
            SegmentType.Thread,
            "Replaces If and Case with StepIf and StepCase if needed and adds Step to enable programming like in C"
        },
        { SegmentType.Step, "Runs operation in separate CLK cycle" },
        {
            SegmentType.Function,
            "Can be used like a normal function with return values\ne.g. Function Add (return NATURAL; x : NATURAL; y : NATURAL) { ... }\n... <= Add(x, y);"
        },
        { SegmentType.NewFunction, "Adds operations and variables from SeqFunction" },
        { SegmentType.Range, "Needed to set size of numbers (e.g. data : INTEGER range 0 to 255)" },
        { SegmentType.Type, "Own datatype\ne.g. TYPE [name] IS (reset, idle, ..., ...);" },
        { SegmentType.Array, "Array of any datatype\nSize e.g. (7 downto 0)\nType e.g. NATURAL RANGE 0 TO 255" },
        { SegmentType.Package, "Needed to use datatypes in other components" },
        {
            SegmentType.SeqFunction,
            "Area in that operations and variables are written for insertion in a process\nIn brackets are connections to variables of the process e.g. (EN, LED)\nSeqFunction [Name] ("
        },
        { SegmentType.When, "Runs operation if value in brackets equals the parameter of case" },
        {
            SegmentType.ParFor,
            "Runs operation \"revisons\" times (i can be used as number that is increased every cycle)\ni IN 0 to [revisions]"
        },
        {
            SegmentType.Return,
            "In Function parameter: \"(return NATURAL; ...\" = Function return NATURAL\nIn code: stops function and return value e.g. return 1;"
        },
        {
            SegmentType.Exit,
            "\"exit;\" Can be used to exit a For or While loop before finished with all revisions (not for While or SeqFor in Thread)"
        },
        {
            SegmentType.Record, "A record is a set of variables that are combined into one\nRead values: ... <= pixel.R"
        },
        {
            SegmentType.SeqFor,
            "First declare a variable, set a condition to stay in the loop\nand set the operation to be executed at the end of every loop (usualy an increment of the variable)\nExample: For (VARIABLE i : INTEGER := 0; i < 8; i := i + 1)"
        },
        { SegmentType.Include, "Allows to select which packages you want to use for this file" },
        {
            SegmentType.Generate,
            "Allows to generate a component or other operations multiple times or if a condition is met"
        },
        { SegmentType.Null, "Performs no action: ... when(others) { null; }" },
        { SegmentType.ParWhile, "Runs operation while condition met. Has to run in a finite amount of cycles" },
        { SegmentType.While, "Runs operation while condition met" }
    };

    private static readonly Dictionary<SegmentType, string> Snippets = new()
    {
        { SegmentType.Main, "Main\n(\n$0\n)\n{\n \n}" },
        { SegmentType.Package, "Package\n(\n$0\n)\n{\n \n}" },
        { SegmentType.Component, "Component\n(\n$0\n)\n{\n \n}" },
        { SegmentType.Generate, "Generate($0)\n{\n \n}" },
        { SegmentType.Thread, "Thread\n{\n$0\n}" },
        { SegmentType.Process, "Process()\n{\n$0\n}" },
        { SegmentType.If, "If($0)" },
        { SegmentType.Elsif, "Elsif($0)" },
        { SegmentType.Else, "Else" },
        { SegmentType.Vhdl, "VHDL\n{\n$0\n}" },
        { SegmentType.For, "For($0)\n{\n \n}" },
        { SegmentType.While, "While($0)\n{\n \n}" },
        { SegmentType.SeqFor, "SeqFor($0)\n{\n \n}" }
    };

    private static readonly Dictionary<SegmentType, string> SnippetsParameter = new()
    {
        { SegmentType.For, "For " },
        { SegmentType.If, "If " },
        { SegmentType.Generic, "Generic\n(\n$0\n);" },
        { SegmentType.Include, "Include\n(\n$0\n);" },
        { SegmentType.Package, "Package\n(\n$0\n);" }
    };

    public static string GetInfo(SegmentType type)
    {
        Info.TryGetValue(type, out var description);
        return description ?? "";
    }

    public static string GetInfoMarkdown(Segment segment)
    {
        var words = segment.NameOrValue.Split(' ');
        switch (segment.SegmentType)
        {
            case SegmentType.Function:
            case SegmentType.VhdlFunction:
                if (AnalyzerHelper.SearchFunction(segment, segment.NameOrValue.ToLower()) is { } function)
                    return FunctionInfo.GetInfoMarkdown(function);
                return "";

            case SegmentType.NewFunction:
                if (AnalyzerHelper.SearchSeqFunction(segment, segment.LastName) is { } seqFunction)
                    return FunctionInfo.GetInfoMarkdown(seqFunction);
                return "";
            case SegmentType.ComponentMember:
                var parent = AnalyzerHelper.SearchTopSegment(segment, SegmentType.NewComponent);
                if (parent != null && segment.Context.AvailableComponents.ContainsKey(parent.LastName.ToLower()))
                {
                    var comp = segment.Context.AvailableComponents[parent.LastName.ToLower()];
                    if (comp.Variables.ContainsKey(segment.NameOrValue.ToLower()))
                    {
                        var variable = comp.Variables[segment.NameOrValue.ToLower()];
                        var comment = GetLineCommentForSegment(variable.Owner);
                        return $"```vhdp{PrintSegment.Convert(variable.Owner)} {comment}\n```";
                    }
                }

                return "";
            case SegmentType.NewComponent:
                if (words.Length == 2 && segment.Context.AvailableComponents.ContainsKey(words[1].ToLower()))
                    return GetComponentInfoMarkdown(segment.Context.AvailableComponents[words[1].ToLower()]);
                return "";
            case SegmentType.DataVariable:
            case SegmentType.VariableDeclaration:
                DefinedVariable? dataVar = null;
                if (segment.ConcatOperator is ".")
                    dataVar = AnalyzerHelper.SearchVariableInRecord(segment);
                else
f                    dataVar = AnalyzerHelper.SearchVariable(segment, segment.NameOrValue);
                if (dataVar != null)
                    return
                        $"```vhdp\n{(segment.SegmentType is SegmentType.VariableDeclaration ? "declaration -> " : "")}{dataVar}\n```";
                return "";
            case SegmentType.Enum:
                var definedEnum = AnalyzerHelper.SearchEnum(segment.Context, segment.NameOrValue);
                if (definedEnum != null) return $"```vhdp\n{segment.NameOrValue} : {definedEnum.Name}\n```";
                return "";
            case SegmentType.TypeUsage:
                return segment.DataType.Description;
            default:
                return GetInfo(segment.SegmentType);
        }
    }

    public static string GetInfoConcatMarkdown(string concatOperator)
    {
        switch (concatOperator)
        {
            case "and":
                return "```vhdp\n0 AND 0 = 0\n0 AND 1 = 0\n1 AND 0 = 0\n1 AND 1 = 1\n```";
            case "or":
                return "```vhdp\n0 OR 0 = 0\n0 OR 1 = 1\n1 OR 0 = 1\n1 OR 1 = 1\n```";
            case "xor":
                return "```vhdp\n0 XOR 0 = 0\n0 XOR 1 = 1\n1 XOR 0 = 1\n1 XOR 1 = 0\n```";
            case "nand":
                return "```vhdp\n0 NAND 0 = 1\n0 NAND 1 = 1\n1 NAND 0 = 1\n1 NAND 1 = 0\n```";
            case "nor":
                return "```vhdp\n0 NOR 0 = 1\n0 NOR 1 = 0\n1 NOR 0 = 0\n1 NOR 1 = 0\n```";
            case "xnor":
                return "```vhdp\n0 XNOR 0 = 1\n0 XNOR 1 = 0\n1 XNOR 0 = 0\n1 XNOR 1 = 1\n```";
            case "not":
                return "```vhdp\nNOT 0 = 1\nNOT 1 = 0\n```";
            default:
                return "";
        }
    }

    public static string GetSnippet(SegmentType type, bool parameter)
    {
        string? snippet;
        if (!parameter) Snippets.TryGetValue(type, out snippet);
        else SnippetsParameter.TryGetValue(type, out snippet);
        return snippet ?? type.ToString();
    }

    public static string GetComponentInsert(Segment comp)
    {
        var str = "";
        var list = comp.Variables.Where(x => x.Value.VariableType is VariableType.Io or VariableType.Generic)
            .OrderByDescending(x => x.Value.VariableType).ToList();
        if (list.Any())
        {
            var gap = false;
            var minLength = list.Max(x => x.Value.Name.Length);
            foreach (var io in list)
            {
                if (!gap && io.Value.VariableType is VariableType.Io)
                {
                    if (io.Key != list.First().Key) str += "\n";
                    gap = true;
                }

                var name = io.Value.Name;
                for (var i = name.Length; i < minLength; i++) name += " ";
                str += "\n" + name + " => $0,";
            }
        }

        return $"New{comp.NameOrValue}\n({str}\n);";
    }

    public static string GetComponentInfoMarkdown(Segment comp)
    {
        var comment = GetCommentForSegment(comp);
        if (!string.IsNullOrEmpty(comment)) comment += '\n';
        var str = "";

        var generics = comp.Variables.Select(x => x.Value).Where(x => x.VariableType is VariableType.Generic).ToList();

        if (generics.Any())
        {
            str += "\n    Generic\n    (";
            foreach (var io in generics)
            {
                var lineComment = GetLineCommentForSegment(io.Owner);
                str += $"\n        {PrintSegment.Convert(io.Owner).Trim()}; {lineComment}";
            }

            str += "\n    );\n";
        }

        foreach (var io in comp.Variables.Select(x => x.Value).Where(x => x is DefinedIo).Cast<DefinedIo>())
        {
            var lineComment = GetLineCommentForSegment(io.Owner);
            str += $"\n    {PrintSegment.Convert(io.Owner).Trim()}; {lineComment}";
        }

        return $"```vhdp\n{comment}New{comp.NameOrValue}\n({str}\n);\n```";
    }

    public static string GetCommentForSegment(Segment s)
    {
        var line = s.Context.GetLine(s.Offset);
        var comment = s.Context.Comments.LastOrDefault(x => s.Context.GetLine(x.Range.End.Value) == line - 1);
        return comment?.Comment ?? "";
    }

    public static string GetLineCommentForSegment(Segment s)
    {
        var line = s.Context.GetLine(s.Offset);
        var comment = s.Context.Comments.LastOrDefault(x => s.Context.GetLine(x.Range.End.Value) == line);
        return comment?.Comment ?? "";
    }
}