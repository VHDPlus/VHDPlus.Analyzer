using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer;

public static class SegmentParser
{
    public static AnalyzerContext Parse(string path, string content)
    {
        AnalyzerContext aC = new(path, content);
        SegmentParserContext context = new(aC, content);

        var preProcessor = false;
        var lineComment = false;
        var lineCommentStartOffset = 0;
        var blockComment = false;
        var blockCommentStartOffset = 0;

        for (context.CurrentIndex = 0; context.CurrentIndex < content.Length; context.CurrentIndex++)
        {
            context.CurrentChar = content[context.CurrentIndex];
            context.NextChar = context.CurrentIndex < content.Length - 1 ? content[context.CurrentIndex + 1] : ' ';
            if (context.CurrentChar is '\r' && context.NextChar is '\n') continue; //Skip windows style newline
            if (context.LastChar is ' ' or '\n' && context.CurrentChar == ' ') continue; //skip indent

            switch (context.CurrentChar) //Detect comments and newline
            {
                case '\n':
                case '\r':
                    if (lineComment)
                        aC.Comments.Add(new FileComment(new Range(lineCommentStartOffset, context.CurrentIndex - 1),
                            content[lineCommentStartOffset..(context.CurrentIndex - 1)]));
                    lineComment = false;
                    preProcessor = false;
                    context.NewLine();
                    context.InString = false;
                    break;
                case '-':
                    if (context.NextChar == '-' && !lineComment && !context.InString)
                    {
                        lineComment = true;
                        lineCommentStartOffset = context.CurrentIndex;
                    }

                    break;
                case '/':
                    if (context.NextChar == '*' && !blockComment && !context.InString)
                    {
                        blockComment = true;
                        blockCommentStartOffset = context.CurrentIndex;
                    }

                    break;
                case '#':
                    if (!context.InString) preProcessor = true;
                    break;
            }

            if (!lineComment && !blockComment &&
                !preProcessor) //Not in a comment & indentation removed & no newline char
            {
                if (context.CurrentChar is '\n') context.CurrentChar = ' ';
                ParseSegment(context);
            }

            switch (context.CurrentChar) //Detect comment block close
            {
                case '/':
                    if (context.LastChar == '*')
                        if (blockComment)
                        {
                            blockComment = false;
                            aC.Comments.Add(new FileComment(
                                new Range(blockCommentStartOffset, context.CurrentIndex + 1),
                                content[blockCommentStartOffset..(context.CurrentIndex + 1)]));
                        }

                    break;
            }

            context.LastChar = context.CurrentChar;
        }

        if (!context.CurrentEmpty()) context.PushSegment();

        if (context.CurrentSegment?.Parent != null)
        {
            context.AnalyzerContext.Diagnostics.Add(new SegmentParserDiagnostic(context.AnalyzerContext,
                "Unexpected end of file",
                DiagnosticLevel.Error, content.Length > 1 ? content.Length - 2 : 0, content.Length - 1));
            while (context.PopBlock())
            {
            }
        }

        return context.AnalyzerContext;
    }

    
    private static void ParseSegment(SegmentParserContext context)
    {
        if (context.InString)
        {
            context.AppendCurrent();
            if (context.CurrentChar is '"') context.InString = false;
            return;
        }

        //Valid characters
        if (context.CurrentChar.IsWordLetter())
        {
            context.AppendCurrent();
            return;
        }

        switch (context.CurrentChar)
        {
            case ' ':
                if(!context.CurrentEmpty()) context.PushSegment();
                break;
            case ';':
                context.PushSegment();
                context.PopSegment();
                break;
            case '{':
                if (!context.CurrentEmpty()) context.PushSegment();
                context.PopTempBlocks();
                break;
            case '}':
                context.PopSegment();
                break;
            default:
                
                break;
        }
    }
}