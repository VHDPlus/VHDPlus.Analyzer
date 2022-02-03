namespace VHDPlus.Analyzer.Elements;

public class ConnectionMember
{
    public ConnectionMember(string from, string? to = null)
    {
        From = from;
        To = to;
    }

    public string From { get; }
    public string? To { get; }
}