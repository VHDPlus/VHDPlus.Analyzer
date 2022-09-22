using VHDPlus.Analyzer.Elements;

namespace VHDPlus.Analyzer.Predefined;

public static class PredefinedFunctions
{
    private static readonly CustomDefinedFunction Integer = new("integer", "Converts Real to Integer")
    {
        ReturnType = DataType.Integer,
        Parameters =
        {
            new FunctionParameter("Source", DataType.Real)
        }
    };

    private static readonly CustomDefinedFunction Real = new("real", "Converts Integer to Real")
    {
        ReturnType = DataType.Real,
        Parameters =
        {
            new FunctionParameter("Source", DataType.Integer)
        }
    };

    private static readonly CustomDefinedFunction Log2 = new("log2", "returns logarithm base 2 of X")
    {
        ReturnType = DataType.Real,
        Parameters =
        {
            new FunctionParameter("X", DataType.Real)
        }
    };

    private static readonly CustomDefinedFunction CeilFromStdLogicVector =
        new("ceil", "returns smallest integer value (as real) not less than X")
        {
            ReturnType = DataType.Real,
            Parameters =
            {
                new FunctionParameter("X", DataType.StdLogicVector)
            }
        };

    private static readonly CustomDefinedFunction CeilFromReal =
        new("ceil", "returns smallest integer value (as real) not less than X")
        {
            ReturnType = DataType.Real,
            Parameters =
            {
                new FunctionParameter("X", DataType.Real)
            }
        };

    private static readonly CustomDefinedFunction ToUnsigned = new("to_unsigned", "Converts Integer to Unsigned")
    {
        ReturnType = DataType.Unsigned,
        Parameters =
        {
            new FunctionParameter("Source", DataType.Integer),
            new FunctionParameter("Length", DataType.Integer)
        }
    };

    private static readonly CustomDefinedFunction Unsigned = new("unsigned", "Converts Vector to Unsigned")
    {
        ReturnType = DataType.Unsigned,
        Parameters =
        {
            new FunctionParameter("Source", DataType.StdLogicVector)
        }
    };

    private static readonly CustomDefinedFunction ToSigned = new("to_signed", "Converts Integer to Signed")
    {
        ReturnType = DataType.Signed,
        Parameters =
        {
            new FunctionParameter("Source", DataType.Integer),
            new FunctionParameter("Length", DataType.Integer)
        }
    };

    private static readonly CustomDefinedFunction Signed = new("signed", "Converts Vector to Signed")
    {
        ReturnType = DataType.Signed,
        Parameters =
        {
            new FunctionParameter("Source", DataType.StdLogicVector)
        }
    };

    private static readonly CustomDefinedFunction ToIntegerFromUnsigned =
        new("to_integer", "Converts Unsigned to Integer")
        {
            ReturnType = DataType.Integer,
            Parameters =
            {
                new FunctionParameter("Source", DataType.Unsigned)
            }
        };

    private static readonly CustomDefinedFunction ToIntegerFromSigned = new("to_integer", "Converts Signed to Integer")
    {
        ReturnType = DataType.Natural,
        Parameters =
        {
            new FunctionParameter("Source", DataType.Signed)
        }
    };

    private static readonly CustomDefinedFunction StdLogicVectorFromSigned =
        new("std_logic_vector", "Converts Signed to STD_LOGIC_VECTOR")
        {
            ReturnType = DataType.StdLogicVector,
            Parameters =
            {
                new FunctionParameter("Source", DataType.Signed)
            }
        };

    private static readonly CustomDefinedFunction StdLogicVectorFromUnsigned =
        new("std_logic_vector", "Converts Unsigned to STD_LOGIC_VECTOR")
        {
            ReturnType = DataType.StdLogicVector,
            Parameters =
            {
                new FunctionParameter("Source", DataType.Unsigned)
            }
        };

    private static readonly CustomDefinedFunction RisingEdge =
        new("rising_edge", "Detects the rising edge of a std_logic signal")
        {
            ReturnType = DataType.Boolean,
            Parameters =
            {
                new FunctionParameter("Signal", DataType.StdLogic)
            }
        };

    private static readonly CustomDefinedFunction FallingEdge =
        new("falling_edge", "Detects the falling edge of a std_logic signal")
        {
            ReturnType = DataType.Boolean,
            Parameters =
            {
                new FunctionParameter("Signal", DataType.StdLogic)
            }
        };

    private static readonly CustomDefinedFunction ResizeFromUnsigned = new("resize", "Resizes the unsigned Vector")
    {
        ReturnType = DataType.Unsigned,
        Parameters =
        {
            new FunctionParameter("Source", DataType.Unsigned),
            new FunctionParameter("New Size", DataType.Integer)
        }
    };

    private static readonly CustomDefinedFunction ResizeFromSigned = new("resize", "Resizes the Signed Vector")
    {
        ReturnType = DataType.Signed,
        Parameters =
        {
            new FunctionParameter("Source", DataType.Signed),
            new FunctionParameter("New Size", DataType.Integer)
        }
    };

    private static readonly CustomDefinedFunction Ext = new("ext", "Zero extend STD_LOGIC_VECTOR")
    {
        ReturnType = DataType.StdLogicVector,
        Parameters =
        {
            new FunctionParameter("Source", DataType.StdLogicVector),
            new FunctionParameter("New Size", DataType.Integer)
        }
    };
    
    private static readonly CustomDefinedFunction Sxt = new("sxt", "Sign extend STD_LOGIC_VECTOR")
    {
        ReturnType = DataType.StdLogicVector,
        Parameters =
        {
            new FunctionParameter("Source", DataType.StdLogicVector),
            new FunctionParameter("New Size", DataType.Integer)
        }
    };

    private static readonly CustomDefinedFunction ShiftRightFromUnsigned =
        new("shift_right", "Performs a shift-right on an UNSIGNED vector COUNT times")
        {
            ReturnType = DataType.Unsigned,
            Parameters =
            {
                new FunctionParameter("Source", DataType.Unsigned),
                new FunctionParameter("Count", DataType.Integer)
            }
        };

    private static readonly CustomDefinedFunction ShiftLeftFromUnsigned =
        new("shift_left", "Performs a shift-left on an UNSIGNED vector COUNT times")
        {
            ReturnType = DataType.Unsigned,
            Parameters =
            {
                new FunctionParameter("Source", DataType.Unsigned),
                new FunctionParameter("Count", DataType.Integer)
            }
        };
    
    private static readonly CustomDefinedFunction ShiftRightFromSigned =
        new("shift_right", "Performs a shift-right on an SIGNED vector COUNT times")
        {
            ReturnType = DataType.Signed,
            Parameters =
            {
                new FunctionParameter("Source", DataType.Signed),
                new FunctionParameter("Count", DataType.Integer)
            }
        };

    private static readonly CustomDefinedFunction ShiftLeftFromSigned =
        new("shift_left", "Performs a shift-left on an SIGNED vector COUNT times")
        {
            ReturnType = DataType.Signed,
            Parameters =
            {
                new FunctionParameter("Source", DataType.Signed),
                new FunctionParameter("Count", DataType.Integer)
            }
        };

    private static readonly CustomDefinedFunction ConvInteger =
        new("conv_integer", "Converts STD_LOGIC_VECTOR to integer")
        {
            ReturnType = DataType.Integer,
            Parameters =
            {
                new FunctionParameter("Source", DataType.StdLogicVector)
            }
        };

    private static readonly CustomDefinedFunction ConvStdLogicVectorFromInteger =
        new("conv_std_logic_vector", "Converts Integer to STD_LOGIC_VECTOR")
        {
            ReturnType = DataType.StdLogicVector,
            Parameters =
            {
                new FunctionParameter("Source", DataType.StdLogicVector),
                new FunctionParameter("Size", DataType.Integer)
            }
        };

    private static readonly CustomDefinedFunction ConvStdLogicVectorFromUnsigned =
        new("conv_std_logic_vector", "Converts Unsigned to STD_LOGIC_VECTOR")
        {
            ReturnType = DataType.StdLogicVector,
            Parameters =
            {
                new FunctionParameter("Source", DataType.Unsigned),
                new FunctionParameter("Size", DataType.Integer)
            }
        };

    private static readonly CustomDefinedFunction ConvStdLogicVectorFromSigned =
        new("conv_std_logic_vector", "Converts Signed to STD_LOGIC_VECTOR")
        {
            ReturnType = DataType.StdLogicVector,
            Parameters =
            {
                new FunctionParameter("Source", DataType.Signed),
                new FunctionParameter("Size", DataType.Integer)
            }
        };

    private static readonly CustomDefinedFunction ToBit = new("to_bit", "Converts STD_LOGIC to Bit")
    {
        ReturnType = DataType.Bit,
        Parameters =
        {
            new FunctionParameter("Source", DataType.StdLogic)
        }
    };

    private static readonly CustomDefinedFunction ToBitVector = new("to_bit", "Converts STD_LOGIC_VECTOR to BIT_VECTOR")
    {
        ReturnType = DataType.BitVector,
        Parameters =
        {
            new FunctionParameter("Source", DataType.StdLogicVector)
        }
    };

    public static readonly Dictionary<string, IEnumerable<CustomDefinedFunction>> StdLogic1164 = new()
    {
        {"rising_edge", new[] {RisingEdge}},
        {"falling_edge", new[] {FallingEdge}},
    };

    public static readonly Dictionary<string, IEnumerable<CustomDefinedFunction>> NumericStd = new()
    {
        { "to_unsigned", new[] { ToUnsigned } },
        { "unsigned", new[] { Unsigned } },
        { "to_signed", new[] { ToSigned } },
        { "signed", new[] { Signed } },
        { "to_integer", new[] { ToIntegerFromSigned, ToIntegerFromUnsigned } },
        { "std_logic_vector", new[] { StdLogicVectorFromSigned, StdLogicVectorFromUnsigned } },
        { "resize", new[] { ResizeFromSigned, ResizeFromUnsigned } },
        { "shift_right", new[] { ShiftRightFromSigned, ShiftRightFromUnsigned } },
        { "shift_left", new[] { ShiftLeftFromSigned, ShiftLeftFromUnsigned } },
        { "to_bit", new[] { ToBit } },
        { "to_bitvector", new[] { ToBitVector } }
    };
    
    public static readonly Dictionary<string, IEnumerable<CustomDefinedFunction>> StdLogicArith = new()
    {
        { "sxt", new[] { Sxt } },
        { "ext", new[] { Ext } },
        { "conv_integer", new[] { ConvInteger } },
        {
            "conv_std_logic_vector",
            new[] { ConvStdLogicVectorFromInteger, ConvStdLogicVectorFromSigned, ConvStdLogicVectorFromUnsigned }
        },
        
    };

    public static readonly Dictionary<string, IEnumerable<CustomDefinedFunction>> MathReal = new()
    {
        { "integer", new[] { Integer } },
        { "real", new[] { Real } },
        { "ceil", new[] { CeilFromReal, CeilFromStdLogicVector } },
        { "log2", new[] { Log2 } }
    };
}