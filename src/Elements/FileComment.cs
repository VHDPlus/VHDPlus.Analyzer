namespace VHDPlus.Analyzer.Elements;

public class FileComment
{
    public FileComment(Range range, string comment)
    {
        Range = range;
        Comment = comment;
    }

    public Range Range { get; }
    public string Comment { get; }
}