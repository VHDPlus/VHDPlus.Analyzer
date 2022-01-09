using System.Text;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Info;

public static class PrintSegment
{
    public static string Convert(Segment start)
    {
        StringBuilder sb = new();

        Convert(start, sb);

        return sb.ToString();
    }

    private static void Convert(Segment start, StringBuilder sb, int depth = 0, bool parameter = false)
    {
        if (start.ConcatSegment)
        {
            if (start.ConcatOperator is "." or "'") sb.Append(start.ConcatOperator);
            else sb.Append($" {start.ConcatOperator} ");
        }
        else
        {
            if (start.SegmentType is SegmentType.Component || !parameter)
            {
                sb.Append("\n");
                for (var i = 0; i < depth; i++) sb.Append("    ");
            }
        }

        sb.Append(start.NameOrValue);
        //parameter
        foreach (var par in start.Parameter)
        {
            sb.Append('(');
            foreach (var p in par) Convert(p, sb, depth, true);
            sb.Append(')');
        }

        var noBracket = !start.Children.Any() || start.Children.First().ConcatSegment;
        if (!parameter)
            if (!start.ConcatSegment && !noBracket)
            {
                sb.Append("\n");
                for (var i = 0; i < depth; i++) sb.Append("    ");
                sb.Append("{");
            }

        foreach (var child in start.Children) Convert(child, sb, depth + (start.ConcatSegment ? 0 : 1));

        if (!start.ConcatSegment)
        {
            if (noBracket)
            {
                if(start.SymSegment)
                    sb.Append(";");
            }
            else
            {
                if (!parameter)
                {
                    sb.Append("\n");
                    for (var i = 0; i < depth; i++) sb.Append("    ");
                    sb.Append("}");
                }
            }
        }
    }
}