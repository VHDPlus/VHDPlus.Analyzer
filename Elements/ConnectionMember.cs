namespace VHDPlus.Analyzer.Elements;

public class ConnectionMember
{
    public string From { get; }
    public string? To { get; }

    public ConnectionMember(string from, string? to = null)
    {
        From = from;
        To = to;
    }
}