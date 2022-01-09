namespace VHDPlus.Analyzer.Elements;

public class FileComment
{
    public Range Range { get; }
    public string Comment { get; }

    public FileComment(Range range, string comment)
    {
        Range = range;
        Comment = comment;
    }
}