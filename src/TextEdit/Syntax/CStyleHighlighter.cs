using System;

namespace ImGuiColorTextEditNet.Syntax;

/// <summary>
/// A syntax highlighter for C and C++ style languages.
/// </summary>
public class CStyleHighlighter : ISyntaxHighlighter
{
    static readonly object DefaultState = new();
    static readonly object MultiLineCommentState = new();
    readonly SimpleTrie<Identifier> _identifiers;

    record Identifier(PaletteIndex Color)
    {
        public string Declaration = "";
    }

    /// <summary>
    /// Creates a new instance of the CStyleHighlighter.
    /// </summary>
    /// <param name="useCpp">true for C++, false for C</param>
    public CStyleHighlighter(bool useCpp)
    {
        var language = useCpp ? CPlusPlus() : C();

        _identifiers = new();
        if (language.Keywords != null)
            foreach (var keyword in language.Keywords)
                _identifiers.Add(keyword, new(PaletteIndex.Keyword));

        if (language.Identifiers != null)
        {
            foreach (var name in language.Identifiers)
            {
                var identifier = new Identifier(PaletteIndex.KnownIdentifier)
                {
                    Declaration = "Built-in function",
                };
                _identifiers.Add(name, identifier);
            }
        }
    }

    /// <summary>Indicates whether the highlighter supports auto-indentation.</summary>
    public bool AutoIndentation => true;

    /// <summary>The maximum number of lines that can be processed in a single frame.</summary>
    public int MaxLinesPerFrame => 1000;

    /// <summary>Retrieves the tooltip for a given identifier.</summary>
    public string? GetTooltip(string id)
    {
        var info = _identifiers.Get(id);
        return info?.Declaration;
    }

    /// <summary>Colorizes a line of text based on C/C++ syntax rules.</summary>
    public object Colorize(Span<Glyph> line, object? state)
    {
        for (int i = 0; i < line.Length; )
        {
            int result = Tokenize(line[i..], ref state);
            Util.Assert(result != 0);

            if (result == -1)
            {
                line[i] = new(line[i].Char, PaletteIndex.Default);
                i++;
            }
            else
                i += result;
        }

        return state ?? DefaultState;
    }

    int Tokenize(Span<Glyph> span, ref object? state)
    {
        int i = 0;

        // Skip leading whitespace
        while (i < span.Length && span[i].Char is ' ' or '\t')
            i++;

        if (i > 0)
            return i;

        int result;
        if ((result = TokenizeMultiLineComment(span, ref state)) != -1)
            return result;

        if ((result = TokenizeSingleLineComment(span)) != -1)
            return result;

        if ((result = TokenizePreprocessorDirective(span)) != -1)
            return result;

        if ((result = TokenizeCStyleString(span)) != -1)
            return result;

        if ((result = TokenizeCStyleCharacterLiteral(span)) != -1)
            return result;

        if ((result = TokenizeCStyleIdentifier(span)) != -1)
            return result;

        if ((result = TokenizeCStyleNumber(span)) != -1)
            return result;

        if ((result = TokenizeCStylePunctuation(span)) != -1)
            return result;

        return -1;
    }

    static int TokenizeMultiLineComment(Span<Glyph> span, ref object? state)
    {
        int i = 0;
        if (
            state != MultiLineCommentState
            && (span[i].Char != '/' || 1 >= span.Length || span[1].Char != '*')
        )
        {
            return -1;
        }

        state = MultiLineCommentState;
        for (; i < span.Length; i++)
        {
            span[i] = new(span[i].Char, PaletteIndex.MultiLineComment);
            if (span[i].Char == '*' && i + 1 < span.Length && span[i + 1].Char == '/')
            {
                i++;
                span[i] = new(span[i].Char, PaletteIndex.MultiLineComment);
                state = DefaultState;
                return i;
            }
        }

        return i;
    }

    static int TokenizeSingleLineComment(Span<Glyph> span)
    {
        if (span[0].Char != '/' || 1 >= span.Length || span[1].Char != '/')
            return -1;

        for (int i = 0; i < span.Length; i++)
            span[i] = new(span[i].Char, PaletteIndex.Comment);

        return span.Length;
    }

    static int TokenizePreprocessorDirective(Span<Glyph> span)
    {
        if (span[0].Char != '#')
            return -1;

        for (int i = 0; i < span.Length; i++)
            span[i] = new(span[i].Char, PaletteIndex.Preprocessor);

        return span.Length;
    }

    // csharpier-ignore-start
    static LanguageDefinition C() =>
        new("C")
        {
            Keywords =
            [
                "auto", "break", "case", "char", "const", "continue", "default", "do", "double", "else",
                "enum", "extern", "float", "for", "goto", "if", "inline", "int", "long", "register",
                "restrict", "return", "short", "signed", "sizeof", "static", "struct", "switch", "typedef", "union",
                "unsigned", "void", "volatile", "while", "_Alignas", "_Alignof", "_Atomic", "_Bool", "_Complex", "_Generic",
                "_Imaginary", "_Noreturn", "_Static_assert", "_Thread_local",
            ],
            Identifiers =
            [
                "abort", "abs", "acos", "asin", "atan", "atexit", "atof", "atoi", "atol", "ceil",
                "clock", "cosh", "ctime", "div", "exit", "fabs", "floor", "fmod", "getchar", "getenv",
                "isalnum", "isalpha", "isdigit", "isgraph", "ispunct", "isspace", "isupper", "kbhit", "log10", "log2",
                "log", "memcmp", "modf", "pow", "putchar", "putenv", "puts", "rand", "remove", "rename",
                "sinh", "sqrt", "srand", "strcat", "strcmp", "strerror", "time", "tolower", "toupper",
            ],
        };

    static LanguageDefinition CPlusPlus() =>
        new("C++")
        {
            Keywords =
            [
                "alignas", "alignof", "and", "and_eq", "asm", "atomic_cancel", "atomic_commit", "atomic_noexcept", "auto", "bitand",
                "bitor", "bool", "break", "case", "catch", "char", "char16_t", "char32_t", "class", "compl",
                "concept", "const", "constexpr", "const_cast", "continue", "decltype", "default", "delete", "do", "double",
                "dynamic_cast", "else", "enum", "explicit", "export", "extern", "false", "float", "for", "friend",
                "goto", "if", "import", "inline", "int", "long", "module", "mutable", "namespace", "new",
                "noexcept", "not", "not_eq", "nullptr", "operator", "or", "or_eq", "private", "protected", "public",
                "register", "reinterpret_cast", "requires", "return", "short", "signed", "sizeof", "static", "static_assert", "static_cast",
                "struct", "switch", "synchronized", "template", "this", "thread_local", "throw", "true", "try", "typedef",
                "typeid", "typename", "union", "unsigned", "using", "virtual", "void", "volatile", "wchar_t", "while",
                "xor", "xor_eq",
            ],
            Identifiers =
            [
                "abort", "abs", "acos", "asin", "atan", "atexit", "atof", "atoi", "atol", "ceil",
                "clock", "cosh", "ctime", "div", "exit", "fabs", "floor", "fmod", "getchar", "getenv",
                "isalnum", "isalpha", "isdigit", "isgraph", "ispunct", "isspace", "isupper", "kbhit", "log10", "log2",
                "log", "memcmp", "modf", "pow", "printf", "sprintf", "snprintf", "putchar", "putenv", "puts",
                "rand", "remove", "rename", "sinh", "sqrt", "srand", "strcat", "strcmp", "strerror", "time",
                "tolower", "toupper", "std", "string", "vector", "map", "unordered_map", "set", "unordered_set", "min",
                "max",
            ],
        };
    // csharpier-ignore-end

    static int TokenizeCStyleString(Span<Glyph> input)
    {
        if (input[0].Char != '"')
            return -1; // No opening quotes

        for (int i = 1; i < input.Length; i++)
        {
            var c = input[i].Char;

            // handle end of string
            if (c == '"')
            {
                for (int j = 0; j < i; j++)
                    input[i] = new(c, PaletteIndex.String);

                return i;
            }

            // handle escape character for "
            if (c == '\\' && i + 1 < input.Length && input[i + 1].Char == '"')
                i++;
        }

        return -1; // No closing quotes
    }

    static int TokenizeCStyleCharacterLiteral(Span<Glyph> input)
    {
        int i = 0;

        if (input[i++].Char != '\'')
            return -1;

        if (i < input.Length && input[i].Char == '\\')
            i++; // handle escape characters

        i++; // Skip actual char

        // handle end of character literal
        if (i >= input.Length || input[i].Char != '\'')
            return -1;

        for (int j = 0; j < i; j++)
            input[j] = new(input[j].Char, PaletteIndex.CharLiteral);

        return i;
    }

    int TokenizeCStyleIdentifier(Span<Glyph> input)
    {
        int i = 0;

        var c = input[i].Char;
        if (!char.IsLetter(c) && c != '_')
            return -1;

        i++;

        for (; i < input.Length; i++)
        {
            c = input[i].Char;
            if (c != '_' && !char.IsLetterOrDigit(c))
                break;
        }

        var info = _identifiers.Get<Glyph>(input[..i], x => x.Char);

        for (int j = 0; j < i; j++)
            input[j] = new(input[j].Char, info?.Color ?? PaletteIndex.Identifier);

        return i;
    }

    static int TokenizeCStyleNumber(Span<Glyph> input)
    {
        int i = 0;
        char c = input[i].Char;

        bool startsWithNumber = char.IsNumber(c);

        if (c != '+' && c != '-' && !startsWithNumber)
            return -1;

        i++;

        bool hasNumber = startsWithNumber;
        while (i < input.Length && char.IsNumber(input[i].Char))
        {
            hasNumber = true;
            i++;
        }

        if (!hasNumber)
            return -1;

        bool isFloat = false;
        bool isHex = false;
        bool isBinary = false;

        if (i < input.Length)
        {
            if (input[i].Char == '.')
            {
                isFloat = true;

                i++;
                while (i < input.Length && char.IsNumber(input[i].Char))
                    i++;
            }
            else if (input[i].Char is 'x' or 'X' && i == 1 && input[i].Char == '0')
            {
                // hex formatted integer of the type 0xef80
                isHex = true;

                i++;
                for (; i < input.Length; i++)
                {
                    c = input[i].Char;
                    if (
                        !char.IsNumber(c)
                        && c is not (>= 'a' and <= 'f')
                        && c is not (>= 'A' and <= 'F')
                    )
                    {
                        break;
                    }
                }
            }
            else if (input[i].Char is 'b' or 'B' && i == 1 && input[i].Char == '0')
            {
                // binary formatted integer of the type 0b01011101

                isBinary = true;

                i++;
                for (; i < input.Length; i++)
                {
                    c = input[i].Char;
                    if (c != '0' && c != '1')
                        break;
                }
            }
        }

        if (!isHex && !isBinary)
        {
            // floating point exponent
            if (i < input.Length && input[i].Char is 'e' or 'E')
            {
                isFloat = true;

                i++;

                if (i < input.Length && input[i].Char is '+' or '-')
                    i++;

                bool hasDigits = false;
                while (i < input.Length && input[i].Char is >= '0' and <= '9')
                {
                    hasDigits = true;
                    i++;
                }

                if (!hasDigits)
                    return -1;
            }

            // single precision floating point type
            if (i < input.Length && input[i].Char == 'f')
                i++;
        }

        if (!isFloat)
        {
            // integer size type
            while (i < input.Length && input[i].Char is 'u' or 'U' or 'l' or 'L')
                i++;
        }

        return i;
    }

    static int TokenizeCStylePunctuation(Span<Glyph> input)
    {
        // csharpier-ignore-start
        switch (input[0].Char)
        {
            case '[': case ']': case '{': case '}': case '(': case ')': case '-': case '+': case '<': case '>': case '?': case ':':
            case ';': case '!': case '%': case '^': case '&': case '|': case '*': case '/': case '=': case '~': case ',': case '.':
                input[0] = new(input[0].Char, PaletteIndex.Punctuation);
                return 1;

            default:
                return -1;
        }
        // csharpier-ignore-end
    }
}
