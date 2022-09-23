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

    public bool InString, IgnoreSegment;

    public SegmentParserContext(AnalyzerContext analyzerContext, string text)
    {
        AnalyzerContext = analyzerContext;
        CurrentSegment = analyzerContext.TopSegment;
        LastChar = '\n';
        LastCharNoWhiteSpace = '\n';
        Text = text;
    }

    public bool VhdlMode { get; set; }

    public AnalyzerContext AnalyzerContext { get; }
    public StringBuilder CurrentInner { get; } = new();
    public int CurrentInnerIndex { get; set; }
    public int LastInnerIndex { get; set; } = 1;
    public int CurrentIndex { get; set; }
    public char LastCharNoWhiteSpace { get; set; }
    public char LastChar { get; set; }
    public char LastLastChar => OffsetChar(-2);
    public char CurrentChar { get; set; }
    public char NextChar { get; set; }
    public char NextNextChar => OffsetChar(2);
    private string Text { get; }

    public Segment CurrentSegment { get; set; }

    public string? CurrentConcatOperator
    {
        get => _currentConcatOperator;
        set
        {
            _currentConcatOperator = value;
            if (value != null) CurrentConcatOperatorIndex = CurrentIndex - value.Length + 1;
        }
    }

    public bool Concat => CurrentConcatOperator != null;
    public int CurrentConcatOperatorIndex { get; set; }
    public int ParameterDepth { get; set; }

    public char OffsetChar(int i)
    {
        i += CurrentIndex;
        if (i >= 0 && i < Text.Length) return Text[i];
        return '\n';
    }

    public bool NextChars(string n)
    {
        if (CurrentIndex + n.Length < Text.Length)
            return Text.Substring(CurrentIndex, n.Length).Equals(n, StringComparison.OrdinalIgnoreCase);
        return false;
    }

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
        if (CurrentInner.Length == 0)
        {
            if (CurrentChar == ' ') return;
            CurrentInnerIndex = CurrentIndex;
        }

        if (CurrentChar != ' ') LastInnerIndex = CurrentIndex;
        CurrentInner.Append(CurrentChar);
    }

    public void SkipIndex(int amount = 1)
    {
        CurrentIndex += amount;
    }

    public void PushSegment()
    {
        var value = GetCurrent().Trim();
        var parameter = CurrentParsePosition == ParsePosition.Parameter && !Concat;

        var (segmentType, dataType) = ParserHelper.CheckSegment(ref value, this, Concat || parameter);

        var newSegment = new Segment(AnalyzerContext, CurrentSegment, value, segmentType, dataType,
            value is "" ? CurrentIndex - 1 : CurrentInnerIndex,
            CurrentConcatOperator, CurrentConcatOperatorIndex);

        var words = value.Split(' ');

        switch (segmentType)
        {
            case SegmentType.Unknown when CurrentChar is not ':' || NextChar is '=':
            case SegmentType.DataVariable when dataType == DataType.Unknown:
                AnalyzerContext.UnresolvedSegments.Add(newSegment);
                break;
            case SegmentType.TypeUsage when dataType == DataType.Unknown:
                AnalyzerContext.UnresolvedTypes.Add(newSegment);
                break;
            case SegmentType.Vhdl:
                VhdlMode = true;
                break;
            case SegmentType.Main:
                var pf = Path.GetFileNameWithoutExtension(AnalyzerContext.FilePath).ToLower();
                newSegment.NameOrValue = "Component " + pf; 
                AnalyzerContext.AddLocalComponent(pf, newSegment);
                break;
            case SegmentType.Component:
                AnalyzerContext.AddLocalComponent(words.Last().ToLower(), newSegment);
                break;
            case SegmentType.Package:
                AnalyzerContext.AddLocalPackage(words.Last().ToLower(), newSegment);
                break;
            case SegmentType.NewFunction:
                AnalyzerContext.UnresolvedSeqFunctions.Add(newSegment);
                break;
            case SegmentType.Function:
                AnalyzerContext.AddLocalFunction(words.Last().ToLower(),
                    new CustomDefinedFunction(words.Last()){Owner = newSegment});
                break;
            case SegmentType.SeqFunction:
                if (words.Length > 0)
                {
                    if (!AnalyzerContext.AvailableSeqFunctions.ContainsKey(words.Last().ToLower()))
                    {
                        AnalyzerContext.AddLocalSeqFunction(words.Last().ToLower(),
                            new CustomDefinedSeqFunction(newSegment, words.Last()));
                    }
                }

                break;
        }

        ClearCurrent();
        CurrentConcatOperator = null;

        if (segmentType is SegmentType.VhdlEnd)
        {
            if (CurrentSegment.SegmentType is SegmentType.Begin or SegmentType.Then) PopBlock();
            PopSegment();
            if (CurrentSegment.SegmentType is SegmentType.While && value.ToLower() is "loop") PopSegment();
            newSegment = new Segment(AnalyzerContext, CurrentSegment, "end " + value, segmentType, dataType,
                newSegment.Offset);
        }

        if (parameter)
        {
            if (!CurrentSegment.Parameter.Any()) CurrentSegment.Parameter.Add(new List<Segment>());
            CurrentSegment.Parameter.Last().Add(newSegment);
        }
        else
        {
            CurrentSegment.Children.Add(newSegment);
            
        }

        CurrentSegment = newSegment;
    }


    public void PopTempBlocks()
    {
        while (CurrentSegment.Parent != null && CurrentSegment.ConcatSegment) PopBlock();
    }

    public bool PopBlock()
    {
        if (CurrentSegment.Parent == null) return false;
        CurrentSegment.EndOffset = CurrentIndex;
        CurrentSegment = CurrentSegment.Parent;
        return true;
    }

    public void PopSegment()
    {
        if (!CurrentEmpty() && CurrentSegment.SegmentType != SegmentType.Connections)
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