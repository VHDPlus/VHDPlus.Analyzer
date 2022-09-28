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
        
    }
}