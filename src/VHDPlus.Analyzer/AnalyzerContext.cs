using System.Collections.ObjectModel;
using VHDPlus.Analyzer.Diagnostics;
using VHDPlus.Analyzer.Elements;
using VHDPlus.Analyzer.Predefined;

namespace VHDPlus.Analyzer;

public class AnalyzerContext
{
    public static readonly AnalyzerContext Empty = new(string.Empty, string.Empty);
    private readonly Dictionary<string, Segment> _availableComponents = new();
    private readonly Dictionary<string, Segment> _availablePackages = new();
    private readonly Dictionary<string, DefinedVariable> _availableExposingVariables = new();
    private readonly Dictionary<string, IEnumerable<CustomDefinedFunction>> _availableFunctions = new();
    private readonly Dictionary<string, CustomDefinedSeqFunction> _availableSeqFunctions = new();
    private readonly Dictionary<string, DataType> _availableTypes = new();
    private readonly Dictionary<string, Segment> _components = new();
    private readonly Dictionary<string, Segment> _packages = new();
    private readonly Dictionary<string, DefinedVariable> _exposingVariables = new();
    private readonly Dictionary<string, IEnumerable<CustomDefinedFunction>> _functions = new();
    private readonly Dictionary<string, CustomDefinedSeqFunction> _seqFunctions = new();
    private readonly Dictionary<string, DataType> _types = new();
    public readonly ReadOnlyDictionary<string, Segment> AvailableComponents;
    public readonly ReadOnlyDictionary<string, Segment> AvailablePackages;
    public readonly ReadOnlyDictionary<string, DefinedVariable> AvailableExposingVariables;
    public readonly ReadOnlyDictionary<string, IEnumerable<CustomDefinedFunction>> AvailableFunctions;
    public readonly ReadOnlyDictionary<string, CustomDefinedSeqFunction> AvailableSeqFunctions;
    public readonly ReadOnlyDictionary<string, DataType> AvailableTypes;
    public readonly List<FileComment> Comments = new();
    public readonly Dictionary<string, ConnectionMember> Connections = new();
    public readonly List<IAnalyzerDiagnostic> Diagnostics = new();
    public readonly string FilePath;
    public bool IncludeExists { get; set; }
    public readonly List<string> Includes = new();
    public readonly List<int> LineOffsets = new() { 0 };
    public readonly Segment TopSegment;
    public readonly List<Segment> UnresolvedComponents = new();
    public readonly List<Segment> UnresolvedSegments = new();
    public readonly List<Segment> UnresolvedSeqFunctions = new();
    public readonly List<Segment> UnresolvedTypes = new();

    public AnalyzerContext(string filepath, string text)
    {
        FilePath = filepath;
        TopSegment = new Segment(this, null, "GlobalScope", SegmentType.GlobalSegment, DataType.Unknown, 0)
        {
            EndOffset = text.Length
        };

        AvailableComponents = new ReadOnlyDictionary<string, Segment>(_availableComponents);
        AvailablePackages = new ReadOnlyDictionary<string, Segment>(_availablePackages);
        AvailableTypes = new ReadOnlyDictionary<string, DataType>(_availableTypes);
        AvailableFunctions = new ReadOnlyDictionary<string, IEnumerable<CustomDefinedFunction>>(_availableFunctions);
        AvailableExposingVariables = new ReadOnlyDictionary<string, DefinedVariable>(_availableExposingVariables);
        AvailableSeqFunctions = new ReadOnlyDictionary<string, CustomDefinedSeqFunction>(_availableSeqFunctions);

        //Default stuff
        if (Path.GetExtension(filepath) != ".ghdp")
            _availableExposingVariables.Add("clk",
                new DefinedIo(TopSegment, "CLK", DataType.StdLogic, VariableType.Io, IoType.In, 0));
        
        AddPackage(PredefinedFunctions.Standard, PredefinedTypes.Standard, new Dictionary<string, DefinedVariable>());
    }

    public IEnumerable<Segment> TopLevels => TopSegment.Children;

    public int GetOffset(int line, int col)
    {
        if (line >= 0 && line < LineOffsets.Count) return LineOffsets[line] + col;
        return -1;
    }

    public int GetCol(int offset)
    {
        var line = LineOffsets.FindLast(x => offset >= x);
        return offset - line - 1;
    }

    public int GetLine(int offset)
    {
        return LineOffsets.FindLastIndex(x => offset >= x);
    }

    public bool InComment(int offset)
    {
        return Comments.Any(c => offset >= c.Range.Start.Value && offset <= c.Range.End.Value);
    }

    public void AddLocalPackage(string key, Segment owner)
    {
        if (!_availablePackages.ContainsKey(key))
        {
            _packages.Add(key, owner);
            _availablePackages.Add(key, owner);
        }
    }
    
    public void AddLocalComponent(string key, Segment owner)
    {
        if (!_availableComponents.ContainsKey(key))
        {
            _components.Add(key, owner);
            //_availableComponents.Add(key, owner);
        }
    }
    
    public void AddLocalType(string key, DataType type, Segment owner)
    {
        if(!AvailableTypes.ContainsKey(key))
        {
            _types.Add(key, type);
            _availableTypes.Add(key, type);
        }
        else
        {
            Diagnostics.Add(
                new SegmentParserDiagnostic(this, $"Type {key} is already defined!", 
                    DiagnosticLevel.Error, owner));
        }
    }

    public void AddLocalFunction(string key, CustomDefinedFunction func)
    {
        if (!_functions.ContainsKey(key))
            _functions.Add(key, new[] { func });
        else _functions[key] = _functions[key].Append(func);
        if (!_availableFunctions.ContainsKey(key))
            _availableFunctions.Add(key, new[] { func });
        else _availableFunctions[key] = _availableFunctions[key].Append(func);
    }

    public void AddLocalSeqFunction(string key, CustomDefinedSeqFunction func)
    {
        _seqFunctions.Add(key, func);
        _availableSeqFunctions.Add(key, func);
    }

    public void AddLocalExposingVariable(string key, DefinedVariable variable)
    {
        _exposingVariables.Add(key, variable);
        _availableExposingVariables.Add(key, variable);
    }

    public void AddProjectContext(ProjectContext pC)
    {
        foreach (var k in pC.Files.SelectMany(f => f._seqFunctions))
            if (!_availableSeqFunctions.ContainsKey(k.Key))
                _availableSeqFunctions.Add(k.Key, k.Value);

        foreach (var k in pC.Files.SelectMany(f => f._components))
        {
            if (!_availableComponents.ContainsKey(k.Key))
                _availableComponents.Add(k.Key, k.Value);
        }
        
        foreach (var k in pC.Files.SelectMany(f => f._packages))
        {
            if (!_availablePackages.ContainsKey(k.Key))
                _availablePackages.Add(k.Key, k.Value);
        }
    }

    public void ResolveIncludes()
    {
        if (!Includes.Any(x => x.StartsWith("ieee.", StringComparison.OrdinalIgnoreCase)))
        {
            ResolveInclude("ieee.numeric_std.all");
            ResolveInclude("ieee.std_logic_1164.all");
            ResolveInclude("ieee.math_real.all");
        }

        if (!IncludeExists)
        {
            foreach (var include in AvailablePackages) ResolveInclude(include.Key + ".all");
        }
        else
        {
            foreach (var include in Includes) ResolveInclude(include);
        }
    }

    private void ResolveInclude(string include)
    {
        include = include.ToLower();
        switch (include.ToLower())
        {
            case "ieee.math_real.all":
                AddPackage(PredefinedFunctions.MathReal, PredefinedTypes.MathReal, new Dictionary<string, DefinedVariable>());
                break;
            case "ieee.numeric_std.all":
                AddPackage(PredefinedFunctions.NumericStd, PredefinedTypes.NumericStd,new Dictionary<string, DefinedVariable>());
                break;
            case "ieee.std_logic_1164.all":
                AddPackage(PredefinedFunctions.StdLogic1164, PredefinedTypes.StdLogic1164,new Dictionary<string, DefinedVariable>());
                break;
            case "ieee.std_logic_arith.all":
                AddPackage(PredefinedFunctions.StdLogicArith, PredefinedTypes.StdLogicArith,new Dictionary<string, DefinedVariable>());
                break;
            default:
                var parts = include.Split('.');
                if (parts.Length > 1)
                {
                    if (AvailablePackages.TryGetValue(parts[0], out var package))
                    {
                        AddPackage(package.Context._functions, package.Context._types, package.Context._exposingVariables);
                    }
                }
                break;
        }
    }

    private void AddPackage(Dictionary<string, IEnumerable<CustomDefinedFunction>> functions, Dictionary<string, DataType> types, Dictionary<string, DefinedVariable> constants)
    {
        //VHDP Default functions
        foreach (var func in functions)
            if (!_availableFunctions.ContainsKey(func.Key))
                _availableFunctions.Add(func.Key, func.Value);
        foreach (var type in types)
            if (!_availableTypes.ContainsKey(type.Key))
                _availableTypes.Add(type.Key, type.Value);
        foreach (var constant in constants)
            if (!_availableExposingVariables.ContainsKey(constant.Key))
                _availableExposingVariables.Add(constant.Key, constant.Value);
    }
}