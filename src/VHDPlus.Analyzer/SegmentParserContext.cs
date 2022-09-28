using System.Text;
using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer;

public enum ParsePosition
{
    Parameter,
    AfterParameter,
    Body
}

public class SegmentParserContext
{
    private string? _currentConcatOperator;
    public ParsePosition CurrentParsePosition = ParsePosition.Body;

    public bool InString;

    public SegmentParserContext(AnalyzerContext analyzerContext, string text)
    {
        AnalyzerContext = analyzerContext;
        LastChar = '\n';
        Text = text;
    }
    
    public AnalyzerContext AnalyzerContext { get; }
    public StringBuilder CurrentInner { get; } = new();
    public int CurrentIndex { get; set; }
    public char LastChar { get; set; }
    public char CurrentChar { get; set; }
    public char NextChar { get; set; }
    private string Text { get; }
    public Segment? CurrentSegment { get; set; }

    private void ClearCurrent()
    {
        CurrentInner.Clear();
    }

    public string GetCurrent(bool clear = false)
    {
        var str = CurrentInner.ToString();
        if (clear) ClearCurrent();
        return str;
    }

    public bool CurrentEmpty()
    {
        return string.IsNullOrWhiteSpace(CurrentInner.ToString());
    }

    public void AppendCurrent()
    {
        CurrentInner.Append(CurrentChar);
    }

    public void PushSegment()
    {
        var value = GetCurrent(true);
        var parameter = false;

        var segmentType = ParserHelper.GetSegmentType(value);

        var newSegment = new Segment()
        {
            SegmentType = segmentType,
            Context = AnalyzerContext,
            Parent = CurrentSegment,
            Offset = CurrentIndex,
            Value = value,
        };

        if (segmentType is SegmentType.Unknown)
        {
            AnalyzerContext.UnresolvedSegments.Add(newSegment);
        }

        if (CurrentSegment == null)
        {
            AnalyzerContext.TopLevels.Add(newSegment);
        }
        else
        {
            if (parameter)
            {
                if (!CurrentSegment.Parameter.Any()) CurrentSegment.Parameter.Add(new List<Segment>());
                CurrentSegment.Parameter.Last().Add(newSegment);
            }
            else
            {
                CurrentSegment.Children.Add(newSegment);
            }
        }

        CurrentSegment = newSegment;
    }


    public void PopTempBlocks()
    {
        while (CurrentSegment is {Parent: { }, Concat: true}) PopBlock();
    }

    public bool PopBlock()
    {
        if (CurrentSegment == null) return false;
        CurrentSegment.EndOffset = CurrentIndex;
        CurrentSegment = CurrentSegment.Parent ?? null;
        return true;
    }

    public void  PopSegment()
    {
        if (!CurrentEmpty() && CurrentSegment?.SegmentType != SegmentType.Connections)
            AnalyzerContext.Diagnostics.Add(new SegmentParserDiagnostic(this, "Unexpected input",
                DiagnosticLevel.Warning));

        PopTempBlocks();

        if (!PopBlock())
            AnalyzerContext.Diagnostics.Add(
                new SegmentParserDiagnostic(this, "Unexpected end", DiagnosticLevel.Error));

        ClearCurrent();
    }

    public void NewLine()
    {
        AnalyzerContext.LineOffsets.Add(CurrentIndex);
    }
}