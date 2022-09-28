using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer;

public static class AnalyzerHelper
{
    public static bool IsWordLetter(this char c)
    {
        return (c == '_' || char.IsLetterOrDigit(c)) && c != ' ';
    }

    public static bool IsDigitOrWhiteSpace(this char c)
    {
        return char.IsWhiteSpace(c) || char.IsDigit(c);
    }

    public static char ToLower(this char c)
    {
        return char.ToLower(c);
    }
}