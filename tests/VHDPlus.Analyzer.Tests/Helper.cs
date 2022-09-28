﻿using System.Linq;
using VHDPlus.Analyzer.Elements;
using Xunit.Abstractions;

namespace VHDPlus.Analyzer.Tests;

public static class Helper
{
    public static void PrintSegment(Segment s, ITestOutputHelper output, int depth = 0)
    {
        var tab = "";
        for (var i = 0; i < depth; i++) tab += "  ";
        
        var str = tab + s.Value + " " +s.Offset + " :" + s.SegmentType + " C: " + s.Concat;
        output.WriteLine(str);
           
        foreach (var par in s.Parameter)
        {
            if(s.Parameter.Any()) output.WriteLine(tab + "(");
            foreach (var p in par)
            {
                PrintSegment(p, output, depth+1);
            }
            if(s.Parameter.Any()) output.WriteLine(tab + ")");
        }
            
        foreach (var se in s.Children)
        {
            PrintSegment(se, output, depth+1);
        }
    }
}