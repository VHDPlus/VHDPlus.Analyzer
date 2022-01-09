using System.Xml.Serialization;
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
                    if (lineComment) aC.Comments.Add(new FileComment(new Range(lineCommentStartOffset, context.CurrentIndex-1), content[lineCommentStartOffset..(context.CurrentIndex-1)]));
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
                    if(!context.InString) preProcessor = true;
                    break;
            }

            if (!lineComment && !blockComment && !preProcessor) //Not in a comment & indentation removed & no newline char
            {
                if (context.CurrentChar is '\n') context.CurrentChar = ' ';
                ParseSegment(context);
            }

            switch (context.CurrentChar) //Detect comment block close
            {
                case '/':
                    if (context.LastChar == '*')
                    {
                        if (blockComment)
                        {
                            blockComment = false;
                            aC.Comments.Add(new FileComment(new Range(blockCommentStartOffset, context.CurrentIndex+1), content[blockCommentStartOffset..(context.CurrentIndex+1)]));
                        }
                    }

                    break;
            }

            context.LastChar = context.CurrentChar;
            if (!char.IsWhiteSpace(context.CurrentChar) && !lineComment && !blockComment)
                context.LastCharNoWhiteSpace = context.CurrentChar;
        }

        if (!context.CurrentEmpty()) context.PushSegment();

        if (context.CurrentSegment.Parent != null)
        {
            context.AnalyzerContext.Diagnostics.Add(new GenericAnalyzerDiagnostic(context.AnalyzerContext, "Unexpected end of file",
                DiagnosticLevel.Error, content.Length > 1 ? content.Length - 2 : 0, content.Length - 1));
            while (context.PopBlock())
            {
            }
        }

        return context.AnalyzerContext;
    }

    private static void ParseSegment(SegmentParserContext context)
    {
        if (context.IgnoreSegment)
        {
            if (context.CurrentChar is '}')
            {
                context.IgnoreSegment = false;
                context.PopBlock();
            }

            return;
        }

        if (context.VhdlMode)
        {
            if (context.CurrentChar is '}')
            {
                context.VhdlMode = false;
            }
        }

        if (context.InString)
        {
            context.AppendCurrent();
            if (context.CurrentChar is '"') context.InString = false;
            return;
        }

        switch (context.CurrentParsePosition)
        {
            case ParsePosition.Body:
                switch (context.CurrentChar.ToLower())
                {
                    case '(':
                        if (context.LastCharNoWhiteSpace is not ')') context.PushSegment();
                        context.CurrentParsePosition = ParsePosition.Parameter;
                        break;
                    case '{':
                        context.CurrentParsePosition = ParsePosition.Body;
                        if (context.CurrentSegment.SegmentType is not SegmentType.Record)
                            context.PushSegment();
                        break;
                    case '}':
                        if (!context.CurrentEmpty())
                        {
                            context.PushSegment();
                            context.PopSegment();
                        }

                        if (context.CurrentConcatOperator != null)
                        {
                            context.CurrentConcatOperator = null;
                            context.PopSegment();
                        }

                        context.PopSegment();
                        break;
                    case ';':
                        if (!context.CurrentEmpty())
                        {
                            context.PushSegment();
                            context.PopSegment();
                        }
                        break;
                    case '+':
                    case '-' when context.NextChar != '\'':
                    case '.':
                    case ',':
                    case '|':
                    case '&': //Concat
                    case '/' when context.NextChar != '=':
                    case ':' when context.NextChar != '=':
                    case '=' when context.NextChar != '>':
                    case '*' when context.NextChar != '*':
                    case '<' when context.NextChar != '=' && context.NextChar != '>':
                    case '>' when context.NextChar != '=' && context.LastChar != '<':
                    case '\'' when !(context.NextNextChar == '\'' &&
                                     ParserHelper.ValidStdLogic.Contains(context.NextChar)) &&
                                   context.LastLastChar != '\'':
                        if (!context.CurrentEmpty() || context.Concat) context.PushSegment();
                        context.CurrentConcatOperator = context.CurrentChar.ToString();
                        break;
                    case ':' when context.NextChar == '=':
                    case '<' when context.NextChar == '=':
                    case '>' when context.NextChar == '=':
                    case '*' when context.NextChar == '*':
                    case '=' when context.NextChar == '>':
                    case '/' when context.NextChar == '=':
                        context.SkipIndex();
                        if (!context.CurrentEmpty() || context.Concat) context.PushSegment();
                        context.CurrentConcatOperator =
                            context.CurrentChar.ToLower() + context.NextChar.ToLower().ToString();
                        break;
                    case '"':
                        context.InString = true;
                        context.AppendCurrent();
                        break;
                    default:
                        if (context.CurrentChar is not ' ')
                        {
                            if (CheckVhdlOperators(context)) break;
                            if (context.LastCharNoWhiteSpace is ')' && !context.VhdlMode)
                                context.CurrentConcatOperator = "{"; //No bracket segment
                        }

                        CheckNormalInput(context);
                        context.AppendCurrent();
                        break;
                }

                break;

            case ParsePosition.Parameter:
                switch (context.CurrentChar)
                {
                    case '(':
                        if (context.LastCharNoWhiteSpace is ')') //Multi dimensional
                        {
                            if (context.CurrentSegment.Children.Any())
                                context.CurrentSegment = context.CurrentSegment.Children.Last();
                            context.CurrentSegment.Parameter.Add(new List<Segment>());
                        }
                        else
                        {
                            context.PushSegment();
                        }

                        context.ParameterDepth++;
                        break;
                    case ')':
                        if (!context.CurrentEmpty()) context.PushSegment();
                        context.CurrentConcatOperator = null;
                        if (context.LastCharNoWhiteSpace is not (';' or '(')) context.PopSegment();

                        if (context.ParameterDepth == 0)
                            context.CurrentParsePosition = ParsePosition.AfterParameter;
                        else
                            context.ParameterDepth--;
                        break;
                    case ';':

                        if (!context.CurrentEmpty() || context.Concat)
                        {
                            context.PushSegment();
                            context.PopSegment();
                        }
                        if (context.LastCharNoWhiteSpace == ')') context.PopSegment();
                        break;
                    case '+':
                    case '-' when context.NextChar != '\'':
                    case '.':
                    case ',':
                    case '|':
                    case '&': //Concat
                    case '/' when context.NextChar != '=':
                    case ':' when context.NextChar != '=':
                    case '<' when context.NextChar != '=' && context.NextChar != '>':
                    case '>' when context.NextChar != '=' && context.LastChar != '<':
                    case '*' when context.NextChar != '*':
                    case '=' when context.NextChar != '>':
                    case '\'' when !(context.NextNextChar == '\'' &&
                                     ParserHelper.ValidStdLogic.Contains(context.NextChar)) &&
                                   context.LastLastChar != '\'':
                        if (!context.CurrentEmpty() || context.Concat || context.LastCharNoWhiteSpace is '(')
                            context.PushSegment();
                        context.CurrentConcatOperator = context.CurrentChar.ToString();
                        break;
                    case ':' when context.NextChar == '=':
                    case '<' when context.NextChar == '=':
                    case '>' when context.NextChar == '=':
                    case '*' when context.NextChar == '*':
                    case '/' when context.NextChar == '=':
                    case '=' when context.NextChar == '>':
                        context.SkipIndex();
                        if (!context.CurrentEmpty() || context.Concat || context.LastCharNoWhiteSpace is '(')
                            context.PushSegment();
                        context.CurrentConcatOperator = context.CurrentChar + context.NextChar.ToString();
                        break;
                    case '"':
                        context.InString = true;
                        context.AppendCurrent();
                        break;
                    default:
                        if (context.CurrentChar is not ' ')
                        {
                            if (CheckVhdlOperators(context)) break;
                            if (context.LastCharNoWhiteSpace is ')')
                                context.PopSegment(); //If concat operator not found
                        }

                        CheckNormalInput(context);
                        context.AppendCurrent();
                        break;
                }

                break;

            case ParsePosition.AfterParameter:
                switch (context.CurrentChar)
                {
                    case '{':
                        if (context.CurrentSegment.Children.Any())
                            context.CurrentSegment = context.CurrentSegment.Children.Last();
                        context.CurrentParsePosition = ParsePosition.Body;
                        break;
                    case '(':
                        context.CurrentParsePosition = ParsePosition.Body;
                        if (context.LastCharNoWhiteSpace is ')') //Multi dimensional
                        {
                            if (context.CurrentSegment.Children.Any())
                                context.CurrentSegment = context.CurrentSegment.Children.Last();
                            context.CurrentSegment.Parameter.Add(new List<Segment>());
                            ParseSegment(context);
                        }

                        break;
                    case ' ':
                        break;
                    case ';':
                        context.CurrentParsePosition = ParsePosition.Body;
                        context.PopSegment();
                        break;
                    default:
                        context.CurrentParsePosition = ParsePosition.Body;
                        ParseSegment(context);
                        break;
                }

                break;
        }
    }

    private static bool CheckVhdlOperators(SegmentParserContext context)
    {
        if (context.LastChar.IsWordLetter()) return false;
        if (ParserHelper.VhdlOperators.Any(op => CheckStringOperator(context, op))) return true;

        if (CheckStringOperator(context, "is")) return true;
        if (CheckStringOperator(context, "of")) return true;
        if (CheckStringOperator(context, "downto")) return true;

        if (ParserHelper.GetLastOperatorSegment(context.CurrentSegment, ":=", "<=") == null) return false;
        return CheckStringOperator(context, "when") || CheckStringOperator(context, "else");
    }

    private static bool CheckStringOperator(SegmentParserContext context, string op)
    {
        if (context.OffsetChar(op.Length).IsWordLetter()) return false;
        if (!context.NextChars(op)) return false;
        context.SkipIndex(op.Length - 1);
        if (!context.CurrentEmpty() || context.LastCharNoWhiteSpace is '(' || context.Concat) context.PushSegment();
        if(!(context.VhdlMode && op is "is" && context.CurrentSegment.SegmentType is SegmentType.Case))
            context.CurrentConcatOperator = op;
        return true;
    }

    private static void CheckNormalInput(SegmentParserContext context)
    {
        if (context.CurrentChar is ' ' or '(')
        {
            var current = context.GetCurrent();
            var words = current.Split(' ');

            if (words.Length > 0)
            {
                var lastWord = words.Last().ToLower();

                if (lastWord == "") return;

                if (lastWord is "to" or "end" || lastWord is "when" && AnalyzerHelper.SearchConcatParent(context.CurrentSegment, SegmentType.Case) is not {SegmentType: SegmentType.Case} ||
                    lastWord == "else" && (context.CurrentConcatOperator == "when" ||
                                           context.CurrentSegment.ConcatOperator == "when") ||
                    ParserHelper.VhdlIos.Contains(lastWord))
                {
                    context.CurrentInner.Remove(context.CurrentInner.Length - lastWord.Length,
                        lastWord.Length);
                    context.LastInnerIndex = context.CurrentInnerIndex + current.Length - lastWord.Length - 1;
                    context.PushSegment();
                    if(lastWord is "end") context.PopSegment();
                    context.CurrentInnerIndex = context.CurrentIndex - lastWord.Length;
                    context.CurrentConcatOperator = lastWord;
                }
                {
                    if (lastWord is "select")
                    {
                        context.CurrentInner.Remove(context.CurrentInner.Length - lastWord.Length,
                            lastWord.Length);
                        context.LastInnerIndex = context.CurrentInnerIndex + current.Length - lastWord.Length - 1;
                        if (words.Length > 1 || context.Concat)
                        {
                            context.PushSegment();
                        }
                        context.CurrentInnerIndex = context.CurrentIndex - lastWord.Length;
                        context.CurrentConcatOperator = lastWord;
                    }
                    if (context.VhdlMode)
                    {
                        if (lastWord is "then" or "begin" or "is" or "generate" or "loop")
                        {
                            context.CurrentInner.Remove(context.CurrentInner.Length - lastWord.Length,
                                lastWord.Length);
                            context.LastInnerIndex = context.CurrentInnerIndex + current.Length - lastWord.Length - 1;
                            if (words.Length > 1 || context.Concat)
                            {
                                context.PushSegment();
                            }
                            context.CurrentInnerIndex = context.CurrentIndex - lastWord.Length;
                        }

                        if (lastWord is "elsif")
                        {
                            if (context.CurrentSegment.SegmentType is SegmentType.Then) context.PopBlock();
                            context.PopTempBlocks();
                            context.PopBlock();
                        }

                        if (lastWord is "else")
                        {
                            if (context.CurrentSegment.SegmentType is SegmentType.Then) context.PopBlock();
                            context.PopTempBlocks();
                            context.PopBlock();
                            context.PushSegment();
                        }
                    }
                    else
                    {
                        if (context.CurrentConcatOperator == "is" && lastWord is "record")
                        {
                            context.PushSegment();
                        }
                    }
                }
            }
        }
    }
}