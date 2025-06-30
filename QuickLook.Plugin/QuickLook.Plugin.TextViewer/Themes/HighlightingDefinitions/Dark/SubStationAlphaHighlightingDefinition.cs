// Copyright © 2017-2025 QL-Win Contributors
//
// This file is part of QuickLook program.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuickLook.Plugin.TextViewer.Themes.HighlightingDefinitions.Dark;

public class SubStationAlphaHighlightingDefinition : DarkHighlightingDefinition
{
    public override string Name => "SubStation Alpha";

    public override string Extension => ".ass;.ssa";

    public override HighlightingRuleSet MainRuleSet => new()
    {
        Rules =
        {
            new HighlightingRule
            {
                Regex = new Regex(@";.*", RegexOptions.Compiled),
                Color = GetNamedColor("Comment")
            },
            new HighlightingRule
            {
                Regex = new Regex(@"!:.*", RegexOptions.Compiled),
                Color = GetNamedColor("Comment")
            },
            new HighlightingRule
            {
                Regex = new Regex(@"Comment:.*", RegexOptions.Compiled),
                Color = GetNamedColor("Comment")
            },
        }
    };

    public override HighlightingColor GetNamedColor(string name)
    {
        return name switch
        {
            "Comment" => new HighlightingColor
            {
                Name = "Comment",
                Foreground = new SimpleHighlightingBrush("#6A9949".ToColor()),
            },
            _ => null,
        };
    }

    public override IEnumerable<HighlightingColor> NamedHighlightingColors =>
    [
        GetNamedColor("Comment"),
    ];

    public override DocumentColorizingTransformer[] LineTransformers { get; } = [new KeyHighlighter()];

    public class KeyHighlighter : DocumentColorizingTransformer
    {
        public Regex SessionRegex { get; } = new(@"\[[^\[\]]*\]", RegexOptions.Compiled);

        public SessionStore Sessions { get; } = [];

        protected override void ColorizeLine(DocumentLine line)
        {
            var text = CurrentContext.Document.GetText(line);
            var textTrimmed = text.TrimStart('\uFEFF').Trim(); // Skip UTF8-BOM (U+FEFF)

            if (string.IsNullOrWhiteSpace(text) || textTrimmed.StartsWith(";") || textTrimmed.StartsWith("Comment:") || textTrimmed.StartsWith("!:"))
                return;

            if (textTrimmed.StartsWith("[") && SessionRegex.IsMatch(textTrimmed))
            {
                var match = Regex.Match(textTrimmed, @"\[(.*?)\]");

                if (match.Success)
                {
                    string sessionName = match.Groups[1].Value;

                    var idxStart = text.IndexOf('[');
                    var idxEnd = text.IndexOf(']');

                    // Session
                    ChangeLinePart(line.Offset + idxStart, line.Offset + idxStart + 1, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush("#FFD705".ToBrush());
                    });
                    ChangeLinePart(line.Offset + idxEnd, line.Offset + idxEnd + 1, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush("#FFD705".ToBrush());
                    });

                    if (!Sessions.ContainsKey(sessionName))
                        Sessions.Add(sessionName, new Session
                        {
                            Name = sessionName,
                            Formats = [],
                            LineNumber = line.LineNumber,
                        });
                    return;
                }
            }

            // Events
            // e.g. Comment: 0,0:00:19.41,0:00:21.87,Style1,,0,0,0,,ABCDEF
            // e.g. Dialogue: 0,0:00:19.41,0:00:21.87,Style2,,0,0,0,,ABCDEF
            if (Sessions.IsCurrentSession("Events", line.LineNumber))
            {
                if (text.StartsWith("Format:"))
                {
                    Sessions["Events"].Formats = [.. text.Substring(text.IndexOf(':')).Split(',').Select(f => f.Trim())];
                    SetFormatForegroundBrush();
                }
                else if (text.StartsWith("Dialogue:"))
                {
                    if (Sessions.ContainsKey("Events"))
                    {
                        SetEventForegroundBrush("Events");
                    }
                }
            }
            // V4 Styles
            // Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, TertiaryColour, BackColour, Bold, Italic, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, AlphaLevel, Encoding
            else if (Sessions.IsCurrentSession("V4 Styles", line.LineNumber))
            {
                if (text.StartsWith("Format:"))
                {
                    Sessions["V4 Styles"].Formats = [.. text.Substring("Format:".Length).Split(',').Select(f => f.Trim())];
                    SetFormatForegroundBrush();
                }
                else if (text.StartsWith("Style:"))
                {
                    if (Sessions.ContainsKey("V4 Styles"))
                    {
                        SetStyleForegroundBrush("V4 Styles");
                    }
                }
            }
            // V4+ Styles
            // Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding, Layer
            else if (Sessions.IsCurrentSession("V4+ Styles", line.LineNumber))
            {
                if (text.StartsWith("Format:"))
                {
                    Sessions["V4+ Styles"].Formats = [.. text.Substring("Format:".Length).Split(',').Select(f => f.Trim())];
                    SetFormatForegroundBrush();
                }
                else if (text.StartsWith("Style:"))
                {
                    if (Sessions.ContainsKey("V4+ Styles"))
                    {
                        SetStyleForegroundBrush("V4+ Styles");
                    }
                }
            }
            // Script Info
            // Aegisub Project Garbage
            else
            {
                SetInfoForegroundBrush();
            }

            // Info
            void SetInfoForegroundBrush()
            {
                int idx = text.IndexOf(':');

                if (idx <= 0)
                    return;

                var val = text.Substring(idx + 1);
                var valTrimmed = val.Trim();
                var type = DetecteType(val);

                // Key
                ChangeLinePart(line.Offset, line.Offset + idx, el =>
                {
                    el.TextRunProperties.SetForegroundBrush("#3F9CD6".ToBrush());
                });

                // Value
                ChangeLinePart(line.Offset + idx + 1, line.Offset + text.Length, el =>
                {
                    el.TextRunProperties.SetForegroundBrush(GetValueColor(type).ToBrush());
                });
            }

            // Style
            void SetStyleForegroundBrush(string sessionName)
            {
                var sessionFormats = Sessions[sessionName].Formats;

                int[] idxes = SpliteIndexes(text, ',');
                int idxPrev = text.IndexOf(':');

                ChangeLinePart(line.Offset, line.Offset + idxPrev, el =>
                {
                    el.TextRunProperties.SetForegroundBrush("#3F9CD6".ToBrush());
                });

                for (int i = default; i < Math.Min(idxes.Length, sessionFormats.Length); i++)
                {
                    int idxStart = idxPrev + 1;
                    int idxEnd = idxPrev = idxes[i];

                    var val = text.Substring(idxStart, idxEnd - idxStart);
                    var valTrimmed = val.Trim();
                    var type = DetecteType(val);

                    if (sessionFormats[i].EndsWith("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        ChangeLinePart(line.Offset + idxStart, line.Offset + idxEnd, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush(GetValueColor(ValueType.String).ToBrush());
                        });
                    }
                    else if (sessionFormats[i].EndsWith("Colour"))
                    {
                        ChangeLinePart(line.Offset + idxStart, line.Offset + idxEnd, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush("#DCDCAA".ToBrush());
                        });
                    }
                    else
                    {
                        ChangeLinePart(line.Offset + idxStart, line.Offset + idxEnd, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush(GetValueColor(type).ToBrush());
                        });
                    }
                }
            }

            // Events
            void SetEventForegroundBrush(string sessionName)
            {
                var sessionFormats = Sessions[sessionName].Formats;

                int[] idxes = SpliteIndexes(text, ',');
                int idxPrev = text.IndexOf(':');

                ChangeLinePart(line.Offset, line.Offset + idxPrev, el =>
                {
                    el.TextRunProperties.SetForegroundBrush("#3F9CD6".ToBrush());
                });

                for (int i = default; i < Math.Min(idxes.Length, sessionFormats.Length); i++)
                {
                    int idxStart = idxPrev + 1;
                    int idxEnd = idxPrev = idxes[i];

                    var val = text.Substring(idxStart, idxEnd - idxStart);
                    var valTrimmed = val.Trim();
                    var type = DetecteType(val);

                    if (sessionFormats[i] == "Name" || sessionFormats[i] == "Style")
                    {
                        ChangeLinePart(line.Offset + idxStart, line.Offset + idxEnd, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush(GetValueColor(ValueType.String).ToBrush());
                        });
                    }
                    else if (sessionFormats[i] == "Start" || sessionFormats[i] == "End")
                    {
                        ChangeLinePart(line.Offset + idxStart, line.Offset + idxEnd, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush("#DCDCAA".ToBrush());
                        });
                    }
                    else if (sessionFormats[i] == "Text")
                    {
                        var valFixed = text.Substring(idxes[i - 1] + 1);
                        SubtitleLine subtitleLine = SubtitleEffectParser.Parse(valFixed);

                        foreach (var item in subtitleLine)
                        {
                            var itemVal = item.Value;

                            if (!string.IsNullOrEmpty(itemVal.Effect))
                            {
                                // Effect All
                                ChangeLinePart(line.Offset + idxStart + itemVal.StartIndex, line.Offset + idxStart + itemVal.EndIndex, el =>
                                {
                                    el.TextRunProperties.SetForegroundBrush("#4EC9A2".ToBrush());
                                });

                                var words = SplitWords(itemVal.Effect);

                                // Effect Text
                                foreach (var word in words)
                                {
                                    var typeEffect = DetecteType(word.Text);

                                    if (typeEffect == ValueType.Numeric)
                                    {
                                        ChangeLinePart(line.Offset + idxStart + itemVal.StartIndex + word.StartIndex, line.Offset + idxStart + itemVal.StartIndex + word.EndIndex, el =>
                                        {
                                            el.TextRunProperties.SetForegroundBrush(GetValueColor(typeEffect).ToBrush());
                                        });
                                    }
                                    else if (typeEffect == ValueType.String)
                                    {
                                        if (word.Text.StartsWith("fn")) // \fn<FontName>
                                        {
                                            ChangeLinePart(line.Offset + idxStart + itemVal.StartIndex + word.StartIndex + 2, line.Offset + idxStart + itemVal.StartIndex + word.EndIndex, el =>
                                            {
                                                el.TextRunProperties.SetForegroundBrush(GetValueColor(typeEffect).ToBrush());
                                            });
                                        }
                                        else if (word.Text.StartsWith("r")) // \r<StyleName>
                                        {
                                            ChangeLinePart(line.Offset + idxStart + itemVal.StartIndex + word.StartIndex + 1, line.Offset + idxStart + itemVal.StartIndex + word.EndIndex, el =>
                                            {
                                                el.TextRunProperties.SetForegroundBrush(GetValueColor(typeEffect).ToBrush());
                                            });
                                        }
                                        else if (word.Text.StartsWith("alpha") // \alpha[&][H][&]
                                              || word.Text.StartsWith("1a") // \1a[&][H][&]
                                              || word.Text.StartsWith("2a") // \2a[&][H][&]
                                              || word.Text.StartsWith("3a") // \3a[&][H][&]
                                              || word.Text.StartsWith("4a") // \4a[&][H][&]
                                              || word.Text.StartsWith("c") // \c[&][H][&] or \c
                                              || word.Text.StartsWith("1c") // \1c[&][H][&]
                                              || word.Text.StartsWith("2c") // \2c[&][H][&]
                                              || word.Text.StartsWith("3c") // \3c[&][H][&]
                                              || word.Text.StartsWith("4c") // \4c[&][H][&]
                                              )
                                        {
                                            var idxAnd = word.Text.IndexOf('&');

                                            if (idxAnd > 0)
                                            {
                                                ChangeLinePart(line.Offset + idxStart + itemVal.StartIndex + word.StartIndex + idxAnd, line.Offset + idxStart + itemVal.StartIndex + word.EndIndex, el =>
                                                {
                                                    el.TextRunProperties.SetForegroundBrush("#DCDCAA".ToBrush());
                                                });
                                            }
                                            else if (word.Text.StartsWith("alpha"))
                                            {
                                                var idxNum = IndexOfFirstDigit(word.Text);

                                                if (idxNum > 0)
                                                {
                                                    ChangeLinePart(line.Offset + idxStart + itemVal.StartIndex + word.StartIndex + idxNum, line.Offset + idxStart + itemVal.StartIndex + word.EndIndex, el =>
                                                    {
                                                        el.TextRunProperties.SetForegroundBrush(GetValueColor(ValueType.Numeric).ToBrush());
                                                    });
                                                }
                                            }
                                        }
                                        else
                                        {
                                            var idxNum = IndexOfFirstDigit(word.Text);

                                            if (idxNum > 0)
                                            {
                                                ChangeLinePart(line.Offset + idxStart + itemVal.StartIndex + word.StartIndex + idxNum, line.Offset + idxStart + itemVal.StartIndex + word.EndIndex, el =>
                                                {
                                                    el.TextRunProperties.SetForegroundBrush(GetValueColor(ValueType.Numeric).ToBrush());
                                                });
                                            }
                                        }
                                    }
                                }

                                // Effect Braces
                                for (int j = default; j < itemVal.Effect.Length; j++)
                                {
                                    var ch = itemVal.Effect[j];

                                    if (ch == '{' || ch == '}')
                                    {
                                        ChangeLinePart(line.Offset + idxStart + itemVal.StartIndex + j, line.Offset + idxStart + itemVal.StartIndex + j + 1, el =>
                                        {
                                            el.TextRunProperties.SetForegroundBrush("#F1D700".ToBrush());
                                        });
                                    }
                                    else if (ch == '(' || ch == ')' || ch == ',')
                                    {
                                        ChangeLinePart(line.Offset + idxStart + itemVal.StartIndex + j, line.Offset + idxStart + itemVal.StartIndex + j + 1, el =>
                                        {
                                            el.TextRunProperties.SetForegroundBrush("#DA70D6".ToBrush());
                                        });
                                    }
                                }
                            }
                            else
                            {
#if flase // Keep text color as default
                                // Text
                                ChangeLinePart(line.Offset + idxStart + itemVal.StartIndex, line.Offset + idxStart + itemVal.EndIndex, el =>
                                {
                                    el.TextRunProperties.SetForegroundBrush(GetValueColor(ValueType.String).ToBrush());
                                });
#endif

                                // Line Breaks (\N or \n) and Space (\h)
                                for (int j = default; j < itemVal.Text.Length; j++)
                                {
                                    var ch = itemVal.Text[j];

                                    if (ch == '\\' && j + 1 < itemVal.Text.Length)
                                    {
                                        var chNext = itemVal.Text[j + 1];

                                        if (chNext == 'N' || chNext == 'n' || chNext == 'h')
                                        {
                                            ChangeLinePart(line.Offset + idxStart + itemVal.StartIndex + j, line.Offset + idxStart + itemVal.StartIndex + j + 2, el =>
                                            {
                                                el.TextRunProperties.SetForegroundBrush("#A0A0A0".ToBrush());
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        ChangeLinePart(line.Offset + idxStart, line.Offset + idxEnd, el =>
                        {
                            el.TextRunProperties.SetForegroundBrush(GetValueColor(type).ToBrush());
                        });
                    }
                }
            }

            // Format
            void SetFormatForegroundBrush()
            {
                int[] idxes = SpliteIndexes(text, ',');
                int idxPrev = text.IndexOf(':');

                ChangeLinePart(line.Offset, line.Offset + idxPrev, el =>
                {
                    el.TextRunProperties.SetForegroundBrush("#3F9CD6".ToBrush());
                });

                for (int i = default; i < idxes.Length; i++)
                {
                    int idxStart = idxPrev + 1;
                    int idxEnd = idxPrev = idxes[i];

                    if (Debugger.IsAttached)
                    {
                        _ = text.Substring(idxStart, idxEnd - idxStart);
                    }

                    ChangeLinePart(line.Offset + idxStart, line.Offset + idxEnd, el =>
                    {
                        el.TextRunProperties.SetForegroundBrush("#3F9CD6".ToBrush());
                    });
                }
            }
        }

        private ValueType DetecteType(string input)
        {
            if (double.TryParse(input, out _))
                return ValueType.Numeric;

            return ValueType.String;
        }

        private string GetValueColor(ValueType type)
        {
            return type switch
            {
                ValueType.Numeric => "#B5CEA8",
                ValueType.String or _ => "#CE9178",
            };
        }

        private static int[] SpliteIndexes(string text, char target)
        {
            if (string.IsNullOrEmpty(text))
                return [];

            var indexes = new List<int>();

            for (int i = default; i < text.Length; i++)
            {
                if (target == text[i])
                {
                    indexes.Add(i);
                }
            }

            // Add the last index to ensure the last segment is captured
            indexes.Add(text.Length);
            return [.. indexes];
        }

        public static InlineText[] SplitWords(string input)
        {
            if (string.IsNullOrEmpty(input))
                return [];

            var result = new List<InlineText>();
            var matches = Regex.Matches(input, @"[^{}\(\)\\,\s]+");
            foreach (Match match in matches)
            {
                result.Add(new InlineText
                {
                    Text = match.Value,
                    StartIndex = match.Index,
                    EndIndex = match.Index + match.Length,
                });
            }

            return [.. result];
        }

        public static int IndexOfFirstDigit(string input)
        {
            if (string.IsNullOrEmpty(input))
                return -1;

            for (int i = 0; i < input.Length; i++)
            {
                if (char.IsDigit(input[i]))
                {
                    if (i > 0 && input[i - 1] == '-')
                        return i - 1;
                    else
                        return i;
                }
            }
            return -1;
        }

        private enum ValueType
        {
            String,
            Numeric,
        }

        public sealed class SessionStore : Dictionary<string, Session>
        {
            public bool IsCurrentSession(string sessionName, int currentLineNumber)
            {
                return ContainsKey(sessionName) && currentLineNumber > this[sessionName].LineNumber;
            }
        }

        [DebuggerDisplay("{ToString()}")]
        public sealed class Session
        {
            public string Name { get; set; }

            public string[] Formats { get; set; }

            public int LineNumber { get; set; }

            public override string ToString()
            {
                return $"[{Name}] {string.Join(", ", Formats)} in line {LineNumber}";
            }
        }

        public static class SubtitleEffectParser
        {
            /// <summary>
            /// 解析ASS字幕行，把特效和正文分块输出
            /// </summary>
            /// <param name="line">ASS原始字符串</param>
            /// <returns>SubtitleLine对象</returns>
            public static SubtitleLine Parse(string line)
            {
                var result = new SubtitleLine();
                int segIdx = 0;
                var regex = new Regex(@"\{.*?\}");

                int lastIndex = 0;
                foreach (Match match in regex.Matches(line))
                {
                    // 处理特效前的文本
                    if (match.Index > lastIndex)
                    {
                        string text = line.Substring(lastIndex, match.Index - lastIndex);
                        if (!string.IsNullOrEmpty(text))
                        {
                            result[segIdx.ToString()] = new SubtitleText
                            {
                                Effect = null,
                                Text = text,
                                StartIndex = lastIndex,
                                EndIndex = match.Index
                            };
                            segIdx++;
                        }
                    }
                    // 处理特效
                    result[segIdx.ToString()] = new SubtitleText
                    {
                        Effect = match.Value,
                        Text = null,
                        StartIndex = match.Index,
                        EndIndex = match.Index + match.Length
                    };
                    segIdx++;
                    lastIndex = match.Index + match.Length;
                }
                // 处理最后的文本
                if (lastIndex < line.Length)
                {
                    string text = line.Substring(lastIndex);
                    if (!string.IsNullOrEmpty(text))
                    {
                        result[segIdx.ToString()] = new SubtitleText
                        {
                            Effect = null,
                            Text = text,
                            StartIndex = lastIndex,
                            EndIndex = line.Length
                        };
                    }
                }
                return result;
            }
        }

        public class SubtitleLine : Dictionary<string, SubtitleText>
        {
        }

        [DebuggerDisplay("{ToString()}")]
        public class SubtitleText
        {
            public string Effect { get; set; }

            public string Text { get; set; }

            public int StartIndex { get; set; }

            public int EndIndex { get; set; }

            public override string ToString()
            {
                return $"{Effect} {Text} [{StartIndex}, {EndIndex}]";
            }
        }

        [DebuggerDisplay("{ToString()}")]
        public class InlineText
        {
            public string Text { get; set; }

            public int StartIndex { get; set; }

            public int EndIndex { get; set; }

            public override string ToString()
            {
                return $"{Text} [{StartIndex}, {EndIndex}]";
            }
        }
    }
}
