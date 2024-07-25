/* TODO: Port to C# or replace w/ new code
namespace ImGuiColorTextEditNet;
public class RegexSyntaxHighlighter : ISyntaxHighlighter
{
    static readonly object DefaultState = new();
    static readonly object MultiLineCommentState = new();
    public bool AutoIndentation { get; }
    public int MaxLinesPerFrame { get; }
    public string? GetTooltip(string id)
    {
        return null;
    }

    public object Colorize(List<Glyph> line, object state)
    {
        return DefaultState;
    }

    void ColorizeRange(int aFromLine = 0, int aToLine = 0)
    {
        if (_lines.Count == 0 || aFromLine >= aToLine)
            return;

        string buffer;
        std::cmatch results;
        string id;

        int endLine = Math.Max(0, Math.Min(_lines.Count, aToLine));
        for (int i = aFromLine; i < endLine; ++i)
        {
            var line = _lines[i];

            if (line.Count == 0)
                continue;

            buffer.resize(line.Count);
            for (int j = 0; j < line.Count; ++j)
            {
                var col = line[j];
                buffer[j] = col._char;
                col._colorIndex = PaletteIndex.Default;
            }

            char* bufferBegin = buffer.front();
            char* bufferEnd = bufferBegin + buffer.Count;

            var last = bufferEnd;

            for (var first = bufferBegin; first != last;)
            {
                char* token_begin = null;
                char* token_end = null;
                PaletteIndex token_color = PaletteIndex.Default;

                bool hasTokenizeResult = false;

                if (_languageDefinition._tokenize != null)
                {
                    if (_languageDefinition._tokenize(first, last, token_begin, token_end, token_color))
                        hasTokenizeResult = true;
                }

                if (!hasTokenizeResult)
                {
                    // todo : remove
                    //printf("using regex for %.*s\n", first + 10 < last ? 10 : int(last - first), first);

                    foreach (var p in _regexList)
                    {
                        if (std::regex_search(first, last, results, p.first, std::regex_constants::match_continuous))
                        {
                            hasTokenizeResult = true;

                            var v = *results.begin();
                            token_begin = v.first;
                            token_end = v.Value;
                            token_color = p.Value;
                            break;
                        }
                    }
                }

                if (!hasTokenizeResult)
                {
                    first++;
                }
                else
                {
                    int token_length = token_end - token_begin;

                    if (token_color == PaletteIndex.Identifier)
                    {
                        id.assign(token_begin, token_end);

                        // todo : allmost all language definitions use lower case to specify keywords, so shouldn't this use ::tolower ?
                        if (!_languageDefinition._caseSensitive)
                            std::transform(id.begin(), id.end(), id.begin(), ::toupper);

                        if (!line[first - bufferBegin]._preprocessor)
                        {
                            if (_languageDefinition._keywords.count(id) != 0)
                                token_color = PaletteIndex.Keyword;
                            else if (_languageDefinition._identifiers.count(id) != 0)
                                token_color = PaletteIndex.KnownIdentifier;
                            else if (_languageDefinition._preprocIdentifiers.count(id) != 0)
                                token_color = PaletteIndex.PreprocIdentifier;
                        }
                        else
                        {
                            if (_languageDefinition._preprocIdentifiers.count(id) != 0)
                                token_color = PaletteIndex.PreprocIdentifier;
                        }
                    }

                    for (int j = 0; j < token_length; ++j)
                        line[(token_begin - bufferBegin) + j]._colorIndex = token_color;

                    first = token_end;
                }
            }
        }
    }

    void ColorizeInternal()
    {
        if (_lines.Count == 0 || !IsColorizerEnabled)
            return;

        if (_checkComments)
        {
            var endLine = _lines.Count;
            var endIndex = 0;
            var commentStartLine = endLine;
            var commentStartIndex = endIndex;
            var withinString = false;
            var withinSingleLineComment = false;
            var withinPreproc = false;
            var firstChar = true;           // there is no other non-whitespace characters in the line before
            var concatenate = false;        // '\' on the very end of the line
            var currentLine = 0;
            var currentIndex = 0;
            while (currentLine < endLine || currentIndex < endIndex)
            {
                var line = _lines[currentLine];

                if (currentIndex == 0 && !concatenate)
                {
                    withinSingleLineComment = false;
                    withinPreproc = false;
                    firstChar = true;
                }

                concatenate = false;

                if (line.Count != 0)
                {
                    var g = line[currentIndex];
                    var c = g._char;

                    if (c != _languageDefinition._preprocChar && !char.IsWhiteSpace(c))
                        firstChar = false;

                    if (currentIndex == line.Count - 1 && line[^1]._char == '\\')
                        concatenate = true;

                    bool inComment = (commentStartLine < currentLine || (commentStartLine == currentLine && commentStartIndex <= currentIndex));

                    if (withinString)
                    {
                        g._multiLineComment = inComment;

                        if (c == '\"')
                        {
                            if (currentIndex + 1 < line.Count && line[currentIndex + 1]._char == '\"')
                            {
                                currentIndex += 1;
                                if (currentIndex < line.Count)
                                    g._multiLineComment = inComment;
                            }
                            else
                                withinString = false;
                        }
                        else if (c == '\\')
                        {
                            currentIndex += 1;
                            if (currentIndex < line.Count)
                                g._multiLineComment = inComment;
                        }
                    }
                    else
                    {
                        if (firstChar && c == _languageDefinition._preprocChar)
                            withinPreproc = true;

                        if (c == '\"')
                        {
                            withinString = true;
                            g._multiLineComment = inComment;
                        }
                        else
                        {
                            bool pred(char a, Glyph b) { return a == b._char; }
                            var from = line.begin() + currentIndex;
                            var startStr = _languageDefinition._commentStart;
                            var singleStartStr = _languageDefinition._singleLineComment;

                            if (singleStartStr.Count > 0 &&
                                currentIndex + singleStartStr.Count <= line.Count &&
                                equals(singleStartStr.begin(), singleStartStr.end(), from, from + singleStartStr.Count, pred))
                            {
                                withinSingleLineComment = true;
                            }
                            else if (!withinSingleLineComment && currentIndex + startStr.Count <= line.Count &&
                                     equals(startStr.begin(), startStr.end(), from, from + startStr.Count, pred))
                            {
                                commentStartLine = currentLine;
                                commentStartIndex = currentIndex;
                            }

                            inComment = inComment = (commentStartLine < currentLine || (commentStartLine == currentLine && commentStartIndex <= currentIndex));

                            g._multiLineComment = inComment;
                            g._comment = withinSingleLineComment;

                            var endStr = _languageDefinition._commentEnd;
                            if (currentIndex + 1 >= (int)endStr.Count &&
                                equals(endStr.begin(), endStr.end(), from + 1 - endStr.Count, from + 1, pred))
                            {
                                commentStartIndex = endIndex;
                                commentStartLine = endLine;
                            }
                        }
                    }

                    g._preprocessor = withinPreproc;
                    line[currentIndex] = g;

                    currentIndex += UTF8CharLength(c);
                    if (currentIndex >= line.Count)
                    {
                        currentIndex = 0;
                        ++currentLine;
                    }
                }
                else
                {
                    currentIndex = 0;
                    ++currentLine;
                }
            }
            _checkComments = false;
        }

        if (_colorRangeMin < _colorRangeMax)
        {
            int increment = (_languageDefinition.Tokenize == null) ? 10 : 10000;
            int to = Math.Min(_colorRangeMin + increment, _colorRangeMax);
            ColorizeRange(_colorRangeMin, to);
            _colorRangeMin = to;

            if (_colorRangeMax == _colorRangeMin)
            {
                _colorRangeMin = int.MaxValue;
                _colorRangeMax = 0;
            }
            return;
        }
    }
}
*/