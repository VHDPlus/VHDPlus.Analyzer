using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer;

public static class SegmentCrawler
{
    public static void GetPairs(Segment start, Action<Segment, Segment, bool, bool> pairCheck)
    {
        var sStack = new Stack<Segment>();
        var ppStack = new Stack<int>();
        var pStack = new Stack<int>();
        var cStack = new Stack<int>();
        sStack.Push(start);
        pStack.Push(0);
        ppStack.Push(0);
        cStack.Push(0);
        var thread = false;
        var threadStartDepth = int.MaxValue;

        while (sStack.Any())
        {
            var s = sStack.Peek();
            var p = pStack.Peek();
            var pp = ppStack.Peek();
            var c = cStack.Peek();

            if (pp < s.Parameter.Count && s.SegmentType is not SegmentType.Generate)
            {
                ppStack.Pop();
                ppStack.Push(pp + 1);
                
                if (p < s.Parameter[pp].Count)
                {
                    pairCheck.Invoke(s, s.Parameter[pp][p], true, thread);
                    sStack.Push(s.Parameter[pp][p]);
                    pStack.Pop();
                    pStack.Push(p+1);
                    pStack.Push(0);
                    cStack.Push(0);
                    ppStack.Push(0);
                    continue;
                }
            }
           
            if (c < s.Children.Count && s.SegmentType is not SegmentType.Vhdl)
            {
                if (!thread && s.SegmentType is SegmentType.Thread or SegmentType.SeqFunction)
                {
                    thread = true;
                    threadStartDepth = sStack.Count;
                }
                pairCheck.Invoke(s, s.Children[c], false, thread);
                sStack.Push(s.Children[c]);
                cStack.Pop();
                cStack.Push(c+1);
                cStack.Push(0);
                pStack.Push(0);
                ppStack.Push(0);
            }
            else
            {
                sStack.Pop();
                cStack.Pop();
                pStack.Pop();
                ppStack.Pop();
                if (threadStartDepth > sStack.Count)
                {
                    thread = false;
                    threadStartDepth = int.MaxValue;
                }
            }
        }
    }
}