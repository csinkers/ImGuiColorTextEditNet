using System.Collections.Generic;

namespace ImGuiColorTextEditNet;

/// <summary>Represents a language definition for syntax highlighting.</summary>
public class LanguageDefinition
{
    /// <summary>The name of the language, used for identification and display purposes.</summary>
    public string Name;

    /// <summary>A list of keywords for the language, used for syntax highlighting.</summary>
    public string[]? Keywords;

    /// <summary>A list of identifiers for the language, which may include built-in functions, types, or other significant terms.</summary>
    public string[]? Identifiers;

    /// <summary>The start and end strings for multi-line comments, used for syntax highlighting.</summary>
    public string CommentStart = "/*";

    /// <summary>The end string for multi-line comments, used for syntax highlighting.</summary>
    public string CommentEnd = "*/";

    /// <summary>The string used for single-line comments, used for syntax highlighting.</summary>
    public string SingleLineComment = "//";

    /// <summary>The character used to denote preprocessor directives, such as `#include` or `#define`.</summary>
    public char PreprocChar = '#';

    /// <summary>Indicates whether the language supports auto-indentation, which can help with formatting code as it is typed.</summary>
    public bool AutoIndentation = true;

    /// <summary>Indicates whether the language is case-sensitive, affecting how keywords and identifiers are matched during syntax highlighting.</summary>
    public bool CaseSensitive = true;

    /// <summary>A list of regular expressions that define token patterns for syntax highlighting.</summary>
    public List<(string, PaletteIndex)> TokenRegexStrings = []; // TODO: Actually use this

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageDefinition"/> class with the specified name.
    /// </summary>
    public LanguageDefinition(string name) => Name = name;

    /// <summary>Creates a predefined language definition for HLSL (High-Level Shading Language).</summary>
    public static LanguageDefinition Hlsl()
    {
        // csharpier-ignore-start
        LanguageDefinition langDef = new("HLSL")
        {
            Keywords =
            [
                "AppendStructuredBuffer", "asm", "asm_fragment", "BlendState", "bool", "break", "Buffer", "ByteAddressBuffer", "case", "cbuffer", "centroid", "class", "column_major", "compile", "compile_fragment",
                "CompileShader", "const", "continue", "ComputeShader", "ConsumeStructuredBuffer", "default", "DepthStencilState", "DepthStencilView", "discard", "do", "double", "DomainShader", "dword", "else",
                "export", "extern", "false", "float", "for", "fxgroup", "GeometryShader", "groupshared", "half", "Hullshader", "if", "in", "inline", "inout", "InputPatch", "int", "interface", "line", "lineadj",
                "linear", "LineStream", "matrix", "min16float", "min10float", "min16int", "min12int", "min16uint", "namespace", "nointerpolation", "noperspective", "NULL", "out", "OutputPatch", "packoffset",
                "pass", "pixelfragment", "PixelShader", "point", "PointStream", "precise", "RasterizerState", "RenderTargetView", "return", "register", "row_major", "RWBuffer", "RWByteAddressBuffer", "RWStructuredBuffer",
                "RWTexture1D", "RWTexture1DArray", "RWTexture2D", "RWTexture2DArray", "RWTexture3D", "sample", "sampler", "SamplerState", "SamplerComparisonState", "shared", "snorm", "stateblock", "stateblock_state",
                "static", "string", "struct", "switch", "StructuredBuffer", "tbuffer", "technique", "technique10", "technique11", "texture", "Texture1D", "Texture1DArray", "Texture2D", "Texture2DArray", "Texture2DMS",
                "Texture2DMSArray", "Texture3D", "TextureCube", "TextureCubeArray", "true", "typedef", "triangle", "triangleadj", "TriangleStream", "uint", "uniform", "unorm", "unsigned", "vector", "vertexfragment",
                "VertexShader", "void", "volatile", "while",
                "bool1","bool2","bool3","bool4","double1","double2","double3","double4", "float1", "float2", "float3", "float4", "int1", "int2", "int3", "int4", "in", "out", "inout",
                "uint1", "uint2", "uint3", "uint4", "dword1", "dword2", "dword3", "dword4", "half1", "half2", "half3", "half4",
                "float1x1","float2x1","float3x1","float4x1","float1x2","float2x2","float3x2","float4x2",
                "float1x3","float2x3","float3x3","float4x3","float1x4","float2x4","float3x4","float4x4",
                "half1x1","half2x1","half3x1","half4x1","half1x2","half2x2","half3x2","half4x2",
                "half1x3","half2x3","half3x3","half4x3","half1x4","half2x4","half3x4","half4x4"
            ],
            Identifiers =
            [
                "abort", "abs", "acos", "all", "AllMemoryBarrier", "AllMemoryBarrierWithGroupSync", "any", "asdouble", "asfloat",
                "asin", "asint", "asuint", "atan", "atan2", "ceil", "CheckAccessFullyMapped", "clamp", "clip", "cos", "cosh",
                "countbits", "cross", "D3DCOLORtoUBYTE4", "ddx", "ddx_coarse", "ddx_fine", "ddy", "ddy_coarse", "ddy_fine",
                "degrees", "determinant", "DeviceMemoryBarrier", "DeviceMemoryBarrierWithGroupSync", "distance", "dot", "dst",
                "errorf", "EvaluateAttributeAtCentroid", "EvaluateAttributeAtSample", "EvaluateAttributeSnapped", "exp", "exp2",
                "f16tof32", "f32tof16", "faceforward", "firstbithigh", "firstbitlow", "floor", "fma", "fmod", "frac", "frexp",
                "fwidth", "GetRenderTargetSampleCount", "GetRenderTargetSamplePosition", "GroupMemoryBarrier",
                "GroupMemoryBarrierWithGroupSync", "InterlockedAdd", "InterlockedAnd", "InterlockedCompareExchange",
                "InterlockedCompareStore", "InterlockedExchange", "InterlockedMax", "InterlockedMin", "InterlockedOr",
                "InterlockedXor", "isfinite", "isinf", "isnan", "ldexp", "length", "lerp", "lit", "log", "log10", "log2", "mad",
                "max", "min", "modf", "msad4", "mul", "noise", "normalize", "pow", "printf", "Process2DQuadTessFactorsAvg",
                "Process2DQuadTessFactorsMax", "Process2DQuadTessFactorsMin", "ProcessIsolineTessFactors",
                "ProcessQuadTessFactorsAvg", "ProcessQuadTessFactorsMax", "ProcessQuadTessFactorsMin", "ProcessTriTessFactorsAvg",
                "ProcessTriTessFactorsMax", "ProcessTriTessFactorsMin", "radians", "rcp", "reflect", "refract", "reversebits",
                "round", "rsqrt", "saturate", "sign", "sin", "sincos", "sinh", "smoothstep", "sqrt", "step", "tan", "tanh",
                "tex1D", "tex1Dbias", "tex1Dgrad", "tex1Dlod", "tex1Dproj", "tex2D", "tex2Dbias", "tex2Dgrad",
                "tex2Dlod", "tex2Dproj", "tex3D", "tex3Dbias", "tex3Dgrad", "tex3Dlod", "tex3Dproj", "texCUBE",
                "texCUBEbias", "texCUBEgrad", "texCUBElod", "texCUBEproj", "transpose", "trunc"
            ]
        };

        langDef.TokenRegexStrings.Add((@"[ \t]*#[ \t]*[a-zA-Z_]+", PaletteIndex.Preprocessor));
        langDef.TokenRegexStrings.Add((@"L?\""(\\.|[^\""])*\""", PaletteIndex.String));
        langDef.TokenRegexStrings.Add((@"\'\\?[^\']\'", PaletteIndex.CharLiteral));
        langDef.TokenRegexStrings.Add(("[+-]?([0-9]+([.][0-9]*)?|[.][0-9]+)([eE][+-]?[0-9]+)?[fF]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("[+-]?[0-9]+[Uu]?[lL]?[lL]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("0[0-7]+[Uu]?[lL]?[lL]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("0[xX][0-9a-fA-F]+[uU]?[lL]?[lL]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("[a-zA-Z_][a-zA-Z0-9_]*", PaletteIndex.Identifier));
        langDef.TokenRegexStrings.Add((@"[\[\]\{\}\!\%\^\&\*\(\)\-\+\=\~\|\<\>\?\/\;\,\.]", PaletteIndex.Punctuation));
        return langDef;
        // csharpier-ignore-end
    }

    /// <summary>Creates a predefined language definition for GLSL (OpenGL Shading Language).</summary>
    public static LanguageDefinition Glsl()
    {
        // csharpier-ignore-start
        LanguageDefinition langDef = new("GLSL")
        {
            Keywords =
            [
                "auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else", "enum", "extern", "float", "for", "goto", "if", "inline", "int", "long", "register", "restrict", "return", "short",
                "signed", "sizeof", "static", "struct", "switch", "typedef", "union", "unsigned", "void", "volatile", "while", "_Alignas", "_Alignof", "_Atomic", "_Bool", "_Complex", "_Generic", "_Imaginary",
                "_Noreturn", "_Static_assert", "_Thread_local"
            ],
            Identifiers =
            [
                "abort", "abs", "acos", "asin", "atan", "atexit", "atof", "atoi", "atol", "ceil", "clock", "cosh", "ctime", "div", "exit", "fabs", "floor", "fmod", "getchar", "getenv", "isalnum", "isalpha", "isdigit", "isgraph",
                "ispunct", "isspace", "isupper", "kbhit", "log10", "log2", "log", "memcmp", "modf", "pow", "putchar", "putenv", "puts", "rand", "remove", "rename", "sinh", "sqrt", "srand", "strcat", "strcmp", "strerror", "time", "tolower", "toupper"
            ]
        };

        langDef.TokenRegexStrings.Add((@"[ \t]*#[ \t]*[a-zA-Z_]+", PaletteIndex.Preprocessor));
        langDef.TokenRegexStrings.Add((@"L?\""(\\.|[^\""])*\""", PaletteIndex.String));
        langDef.TokenRegexStrings.Add((@"\'\\?[^\']\'", PaletteIndex.CharLiteral));
        langDef.TokenRegexStrings.Add(("[+-]?([0-9]+([.][0-9]*)?|[.][0-9]+)([eE][+-]?[0-9]+)?[fF]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("[+-]?[0-9]+[Uu]?[lL]?[lL]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("0[0-7]+[Uu]?[lL]?[lL]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("0[xX][0-9a-fA-F]+[uU]?[lL]?[lL]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("[a-zA-Z_][a-zA-Z0-9_]*", PaletteIndex.Identifier));
        langDef.TokenRegexStrings.Add((@"[\[\]\{\}\!\%\^\&\*\(\)\-\+\=\~\|\<\>\?\/\;\,\.]", PaletteIndex.Punctuation));

        return langDef;
        // csharpier-ignore-end
    }

    /// <summary>Creates a predefined language definition for SQL (Structured Query Language).</summary>
    public static LanguageDefinition Sql()
    {
        // csharpier-ignore-start
        LanguageDefinition langDef = new("SQL")
        {
            CaseSensitive = false,
            AutoIndentation = false,
            Keywords =
            [
                "ADD", "EXCEPT", "PERCENT", "ALL", "EXEC", "PLAN", "ALTER", "EXECUTE", "PRECISION", "AND", "EXISTS", "PRIMARY", "ANY", "EXIT", "PRINT", "AS", "FETCH", "PROC", "ASC", "FILE", "PROCEDURE",
                "AUTHORIZATION", "FILLFACTOR", "PUBLIC", "BACKUP", "FOR", "RAISERROR", "BEGIN", "FOREIGN", "READ", "BETWEEN", "FREETEXT", "READTEXT", "BREAK", "FREETEXTTABLE", "RECONFIGURE",
                "BROWSE", "FROM", "REFERENCES", "BULK", "FULL", "REPLICATION", "BY", "FUNCTION", "RESTORE", "CASCADE", "GOTO", "RESTRICT", "CASE", "GRANT", "RETURN", "CHECK", "GROUP", "REVOKE",
                "CHECKPOINT", "HAVING", "RIGHT", "CLOSE", "HOLDLOCK", "ROLLBACK", "CLUSTERED", "IDENTITY", "ROWCOUNT", "COALESCE", "IDENTITY_INSERT", "ROWGUIDCOL", "COLLATE", "IDENTITYCOL", "RULE",
                "COLUMN", "IF", "SAVE", "COMMIT", "IN", "SCHEMA", "COMPUTE", "INDEX", "SELECT", "CONSTRAINT", "INNER", "SESSION_USER", "CONTAINS", "INSERT", "SET", "CONTAINSTABLE", "INTERSECT", "SETUSER",
                "CONTINUE", "INTO", "SHUTDOWN", "CONVERT", "IS", "SOME", "CREATE", "JOIN", "STATISTICS", "CROSS", "KEY", "SYSTEM_USER", "CURRENT", "KILL", "TABLE", "CURRENT_DATE", "LEFT", "TEXTSIZE",
                "CURRENT_TIME", "LIKE", "THEN", "CURRENT_TIMESTAMP", "LINENO", "TO", "CURRENT_USER", "LOAD", "TOP", "CURSOR", "NATIONAL", "TRAN", "DATABASE", "NOCHECK", "TRANSACTION",
                "DBCC", "NONCLUSTERED", "TRIGGER", "DEALLOCATE", "NOT", "TRUNCATE", "DECLARE", "NULL", "TSEQUAL", "DEFAULT", "NULLIF", "UNION", "DELETE", "OF", "UNIQUE", "DENY", "OFF", "UPDATE",
                "DESC", "OFFSETS", "UPDATETEXT", "DISK", "ON", "USE", "DISTINCT", "OPEN", "USER", "DISTRIBUTED", "OPENDATASOURCE", "VALUES", "DOUBLE", "OPENQUERY", "VARYING","DROP", "OPENROWSET", "VIEW",
                "DUMMY", "OPENXML", "WAITFOR", "DUMP", "OPTION", "WHEN", "ELSE", "OR", "WHERE", "END", "ORDER", "WHILE", "ERRLVL", "OUTER", "WITH", "ESCAPE", "OVER", "WRITETEXT"
            ],
            Identifiers =
            [
                "ABS",  "ACOS",  "ADD_MONTHS",  "ASCII",  "ASCIISTR",  "ASIN",  "ATAN",  "ATAN2",  "AVG",  "BFILENAME",  "BIN_TO_NUM",  "BITAND",  "CARDINALITY",  "CASE",  "CAST",  "CEIL",
                "CHARTOROWID",  "CHR",  "COALESCE",  "COMPOSE",  "CONCAT",  "CONVERT",  "CORR",  "COS",  "COSH",  "COUNT",  "COVAR_POP",  "COVAR_SAMP",  "CUME_DIST",  "CURRENT_DATE",
                "CURRENT_TIMESTAMP",  "DBTIMEZONE",  "DECODE",  "DECOMPOSE",  "DENSE_RANK",  "DUMP",  "EMPTY_BLOB",  "EMPTY_CLOB",  "EXP",  "EXTRACT",  "FIRST_VALUE",  "FLOOR",  "FROM_TZ",  "GREATEST",
                "GROUP_ID",  "HEXTORAW",  "INITCAP",  "INSTR",  "INSTR2",  "INSTR4",  "INSTRB",  "INSTRC",  "LAG",  "LAST_DAY",  "LAST_VALUE",  "LEAD",  "LEAST",  "LENGTH",  "LENGTH2",  "LENGTH4",
                "LENGTHB",  "LENGTHC",  "LISTAGG",  "LN",  "LNNVL",  "LOCALTIMESTAMP",  "LOG",  "LOWER",  "LPAD",  "LTRIM",  "MAX",  "MEDIAN",  "MIN",  "MOD",  "MONTHS_BETWEEN",  "NANVL",  "NCHR",
                "NEW_TIME",  "NEXT_DAY",  "NTH_VALUE",  "NULLIF",  "NUMTODSINTERVAL",  "NUMTOYMINTERVAL",  "NVL",  "NVL2",  "POWER",  "RANK",  "RAWTOHEX",  "REGEXP_COUNT",  "REGEXP_INSTR",
                "REGEXP_REPLACE",  "REGEXP_SUBSTR",  "REMAINDER",  "REPLACE",  "ROUND",  "ROWNUM",  "RPAD",  "RTRIM",  "SESSIONTIMEZONE",  "SIGN",  "SIN",  "SINH",
                "SOUNDEX",  "SQRT",  "STDDEV",  "SUBSTR",  "SUM",  "SYS_CONTEXT",  "SYSDATE",  "SYSTIMESTAMP",  "TAN",  "TANH",  "TO_CHAR",  "TO_CLOB",  "TO_DATE",  "TO_DSINTERVAL",  "TO_LOB",
                "TO_MULTI_BYTE",  "TO_NCLOB",  "TO_NUMBER",  "TO_SINGLE_BYTE",  "TO_TIMESTAMP",  "TO_TIMESTAMP_TZ",  "TO_YMINTERVAL",  "TRANSLATE",  "TRIM",  "TRUNC", "TZ_OFFSET",  "UID",  "UPPER",
                "USER",  "USERENV",  "VAR_POP",  "VAR_SAMP",  "VARIANCE",  "VSIZE "
            ]
        };

        langDef.TokenRegexStrings.Add((@"L?\""(\\.|[^\""])*\""", PaletteIndex.String));
        langDef.TokenRegexStrings.Add((@"\'[^\']*\'", PaletteIndex.String));
        langDef.TokenRegexStrings.Add(("[+-]?([0-9]+([.][0-9]*)?|[.][0-9]+)([eE][+-]?[0-9]+)?[fF]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("[+-]?[0-9]+[Uu]?[lL]?[lL]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("0[0-7]+[Uu]?[lL]?[lL]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("0[xX][0-9a-fA-F]+[uU]?[lL]?[lL]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("[a-zA-Z_][a-zA-Z0-9_]*", PaletteIndex.Identifier));
        langDef.TokenRegexStrings.Add((@"[\[\]\{\}\!\%\^\&\*\(\)\-\+\=\~\|\<\>\?\/\;\,\.]", PaletteIndex.Punctuation));

        return langDef;
        // csharpier-ignore-end
    }

    /// <summary>Creates a predefined language definition for Lua</summary>
    public static LanguageDefinition Lua()
    {
        // csharpier-ignore-start
        LanguageDefinition langDef = new("Lua")
        {
            CommentStart = "--[[",
            CommentEnd = "]]",
            SingleLineComment = "--",
            CaseSensitive = true,
            AutoIndentation = false,
            Keywords =
            [
                "and", "break", "do", "", "else", "elseif", "end", "false", "for", "function", "if", "in", "", "local", "nil", "not", "or", "repeat", "return", "then", "true", "until", "while"
            ],
            Identifiers =
            [
                "assert", "collectgarbage", "dofile", "error", "getmetatable", "ipairs", "loadfile", "load", "loadstring",  "next",  "pairs",  "pcall",  "print",  "rawequal",  "rawlen",  "rawget",  "rawset",
                "select",  "setmetatable",  "tonumber",  "tostring",  "type",  "xpcall",  "_G",  "_VERSION","arshift", "band", "bnot", "bor", "bxor", "btest", "extract", "lrotate", "lshift", "replace",
                "rrotate", "rshift", "create", "resume", "running", "status", "wrap", "yield", "isyieldable", "debug","getuservalue", "gethook", "getinfo", "getlocal", "getregistry", "getmetatable",
                "getupvalue", "upvaluejoin", "upvalueid", "setuservalue", "sethook", "setlocal", "setmetatable", "setupvalue", "traceback", "close", "flush", "input", "lines", "open", "output", "popen",
                "read", "tmpfile", "type", "write", "close", "flush", "lines", "read", "seek", "setvbuf", "write", "__gc", "__tostring", "abs", "acos", "asin", "atan", "ceil", "cos", "deg", "exp", "tointeger",
                "floor", "fmod", "ult", "log", "max", "min", "modf", "rad", "random", "randomseed", "sin", "sqrt", "string", "tan", "type", "atan2", "cosh", "sinh", "tanh",
                "pow", "frexp", "ldexp", "log10", "pi", "huge", "maxinteger", "mininteger", "loadlib", "searchpath", "seeall", "preload", "cpath", "path", "searchers", "loaded", "module", "require", "clock",
                "date", "difftime", "execute", "exit", "getenv", "remove", "rename", "setlocale", "time", "tmpname", "byte", "char", "dump", "find", "format", "gmatch", "gsub", "len", "lower", "match", "rep",
                "reverse", "sub", "upper", "pack", "packsize", "unpack", "concat", "maxn", "insert", "pack", "unpack", "remove", "move", "sort", "offset", "codepoint", "char", "len", "codes", "charpattern",
                "coroutine", "table", "io", "os", "string", "utf8", "bit32", "math", "debug", "package"
            ]
        };

        langDef.TokenRegexStrings.Add((@"L?\""(\\.|[^\""])*\""", PaletteIndex.String));
        langDef.TokenRegexStrings.Add((@"\'[^\']*\'", PaletteIndex.String));
        langDef.TokenRegexStrings.Add(("0[xX][0-9a-fA-F]+[uU]?[lL]?[lL]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("[+-]?([0-9]+([.][0-9]*)?|[.][0-9]+)([eE][+-]?[0-9]+)?[fF]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("[+-]?[0-9]+[Uu]?[lL]?[lL]?", PaletteIndex.Number));
        langDef.TokenRegexStrings.Add(("[a-zA-Z_][a-zA-Z0-9_]*", PaletteIndex.Identifier));
        langDef.TokenRegexStrings.Add((@"[\[\]\{\}\!\%\^\&\*\(\)\-\+\=\~\|\<\>\?\/\;\,\.]", PaletteIndex.Punctuation));

        return langDef;
        // csharpier-ignore-end
    }
}
